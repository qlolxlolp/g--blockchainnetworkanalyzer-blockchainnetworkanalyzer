"""
Scan Scheduler Module - APScheduler integration for automated scanning.
Handles recurring and scheduled scan jobs.
"""

import logging
from typing import Dict, List, Optional, Callable, Any
from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum

try:
    from apscheduler.schedulers.background import BackgroundScheduler
    from apscheduler.triggers.cron import CronTrigger
    from apscheduler.triggers.interval import IntervalTrigger
    from apscheduler.triggers.date import DateTrigger
    from apscheduler.events import EVENT_JOB_EXECUTED, EVENT_JOB_ERROR
    from apscheduler.jobstores.sqlalchemy import SQLAlchemyJobStore
    from apscheduler.executors.pool import ThreadPoolExecutor
    APSCHEDULER_AVAILABLE = True
except ImportError:
    APSCHEDULER_AVAILABLE = False
    logging.warning("APScheduler not available. Install with: pip install APScheduler")

from .database import get_db_manager
from .config_manager import get_config_manager

logger = logging.getLogger(__name__)


class ScheduleFrequency(Enum):
    """Schedule frequency options."""
    ONCE = "once"
    HOURLY = "hourly"
    DAILY = "daily"
    WEEKLY = "weekly"
    MONTHLY = "monthly"
    CUSTOM = "custom"


@dataclass
class ScheduledScan:
    """Represents a scheduled scan configuration."""
    id: Optional[str] = None
    name: str = ""
    cidr_range: str = ""
    frequency: ScheduleFrequency = ScheduleFrequency.DAILY
    enabled: bool = True
    ports: List[int] = field(default_factory=list)
    timeout: int = 3
    concurrency: int = 50
    export_reports: bool = True
    generate_map: bool = False
    notify_on_completion: bool = False
    email_recipients: List[str] = field(default_factory=list)
    webhook_url: str = ""
    cron_expression: str = ""  # For custom schedules
    next_run: Optional[datetime] = None
    last_run: Optional[datetime] = None
    last_status: str = "never"
    run_count: int = 0
    created_at: Optional[datetime] = None


@dataclass
class ScanExecution:
    """Result of a scheduled scan execution."""
    schedule_id: str
    schedule_name: str
    execution_time: datetime
    status: str  # running, completed, failed, cancelled
    hosts_scanned: int = 0
    miners_detected: int = 0
    duration_seconds: float = 0.0
    error_message: str = ""
    scan_id: Optional[int] = None


class ScanScheduler:
    """
    Scan scheduler using APScheduler for automated scanning.
    Supports cron expressions, intervals, and one-time schedules.
    """
    
    def __init__(self, db_path: Optional[str] = None):
        """
        Initialize scan scheduler.
        
        Args:
            db_path: Database path for job persistence (default: data/scheduler.db)
        """
        if not APSCHEDULER_AVAILABLE:
            raise RuntimeError("APScheduler is required. Install with: pip install APScheduler")
        
        self.db = get_db_manager()
        self.scheduled_scans: Dict[str, ScheduledScan] = {}
        self.execution_history: List[ScanExecution] = []
        self._scan_callbacks: Dict[str, Callable] = {}
        
        # Configure APScheduler
        db_path = db_path or "data/scheduler.db"
        jobstores = {
            'default': SQLAlchemyJobStore(url=f'sqlite:///{db_path}')
        }
        
        executors = {
            'default': ThreadPoolExecutor(10)
        }
        
        job_defaults = {
            'coalesce': False,
            'max_instances': 1,
            'misfire_grace_time': 3600  # 1 hour grace
        }
        
        self.scheduler = BackgroundScheduler(
            jobstores=jobstores,
            executors=executors,
            job_defaults=job_defaults,
            timezone='UTC'
        )
        
        # Add event listeners
        self.scheduler.add_listener(self._job_executed, EVENT_JOB_EXECUTED)
        self.scheduler.add_listener(self._job_error, EVENT_JOB_ERROR)
        
        logger.info("Scan scheduler initialized")
    
    def start(self) -> None:
        """Start the scheduler."""
        self.scheduler.start()
        logger.info("Scan scheduler started")
    
    def shutdown(self) -> None:
        """Shutdown the scheduler."""
        self.scheduler.shutdown()
        logger.info("Scan scheduler shutdown")
    
    def add_schedule(self, schedule: ScheduledScan, 
                    scan_callback: Callable) -> bool:
        """
        Add a new scheduled scan.
        
        Args:
            schedule: ScheduledScan configuration
            scan_callback: Callback function to execute when scan runs
            
        Returns:
            True if schedule was added successfully
        """
        try:
            # Generate schedule ID if not provided
            if not schedule.id:
                schedule.id = f"scan_{datetime.now().strftime('%Y%m%d%H%M%S')}"
            
            # Determine trigger based on frequency
            trigger = self._create_trigger(schedule)
            
            if trigger is None:
                logger.error(f"Invalid trigger for schedule: {schedule.name}")
                return False
            
            # Add job to scheduler
            job = self.scheduler.add_job(
                func=scan_callback,
                trigger=trigger,
                id=schedule.id,
                name=schedule.name,
                args=[schedule],
                replace_existing=True
            )
            
            # Store schedule configuration
            schedule.next_run = job.next_run_time
            schedule.created_at = datetime.now()
            self.scheduled_scans[schedule.id] = schedule
            self._scan_callbacks[schedule.id] = scan_callback
            
            # Store in database
            self._save_schedule_to_db(schedule)
            
            logger.info(f"Added schedule: {schedule.name} (ID: {schedule.id})")
            logger.info(f"Next run: {schedule.next_run}")
            
            return True
        
        except Exception as e:
            logger.error(f"Failed to add schedule: {e}")
            return False
    
    def _create_trigger(self, schedule: ScheduledScan):
        """
        Create APScheduler trigger from schedule configuration.
        
        Args:
            schedule: ScheduledScan configuration
            
        Returns:
            Trigger object or None if invalid
        """
        if schedule.frequency == ScheduleFrequency.ONCE:
            return DateTrigger(run_date=schedule.next_run)
        
        elif schedule.frequency == ScheduleFrequency.HOURLY:
            return IntervalTrigger(hours=1)
        
        elif schedule.frequency == ScheduleFrequency.DAILY:
            return IntervalTrigger(days=1)
        
        elif schedule.frequency == ScheduleFrequency.WEEKLY:
            return IntervalTrigger(weeks=1)
        
        elif schedule.frequency == ScheduleFrequency.MONTHLY:
            return IntervalTrigger(months=1)
        
        elif schedule.frequency == ScheduleFrequency.CUSTOM:
            if schedule.cron_expression:
                try:
                    # Parse cron expression (e.g., "0 2 * * *" for daily at 2 AM)
                    parts = schedule.cron_expression.split()
                    if len(parts) == 5:
                        # Standard cron: minute hour day month day_of_week
                        return CronTrigger(
                            minute=parts[0],
                            hour=parts[1],
                            day=parts[2],
                            month=parts[3],
                            day_of_week=parts[4]
                        )
                except Exception as e:
                    logger.error(f"Invalid cron expression: {e}")
            return None
        
        return None
    
    def remove_schedule(self, schedule_id: str) -> bool:
        """
        Remove a scheduled scan.
        
        Args:
            schedule_id: ID of schedule to remove
            
        Returns:
            True if removed successfully
        """
        try:
            self.scheduler.remove_job(schedule_id)
            
            if schedule_id in self.scheduled_scans:
                del self.scheduled_scans[schedule_id]
            
            if schedule_id in self._scan_callbacks:
                del self._scan_callbacks[schedule_id]
            
            self._delete_schedule_from_db(schedule_id)
            
            logger.info(f"Removed schedule: {schedule_id}")
            return True
        
        except Exception as e:
            logger.error(f"Failed to remove schedule: {e}")
            return False
    
    def pause_schedule(self, schedule_id: str) -> bool:
        """Pause a scheduled scan."""
        try:
            self.scheduler.pause_job(schedule_id)
            if schedule_id in self.scheduled_scans:
                self.scheduled_scans[schedule_id].enabled = False
            logger.info(f"Paused schedule: {schedule_id}")
            return True
        except Exception as e:
            logger.error(f"Failed to pause schedule: {e}")
            return False
    
    def resume_schedule(self, schedule_id: str) -> bool:
        """Resume a paused scheduled scan."""
        try:
            self.scheduler.resume_job(schedule_id)
            if schedule_id in self.scheduled_scans:
                self.scheduled_scans[schedule_id].enabled = True
            logger.info(f"Resumed schedule: {schedule_id}")
            return True
        except Exception as e:
            logger.error(f"Failed to resume schedule: {e}")
            return False
    
    def run_schedule_now(self, schedule_id: str) -> bool:
        """Execute a scheduled scan immediately."""
        try:
            schedule = self.scheduled_scans.get(schedule_id)
            if not schedule:
                logger.error(f"Schedule not found: {schedule_id}")
                return False
            
            callback = self._scan_callbacks.get(schedule_id)
            if not callback:
                logger.error(f"Callback not found for schedule: {schedule_id}")
                return False
            
            # Execute callback
            callback(schedule)
            
            logger.info(f"Manually executed schedule: {schedule_id}")
            return True
        
        except Exception as e:
            logger.error(f"Failed to run schedule: {e}")
            return False
    
    def get_schedule(self, schedule_id: str) -> Optional[ScheduledScan]:
        """Get a scheduled scan by ID."""
        return self.scheduled_scans.get(schedule_id)
    
    def get_all_schedules(self) -> List[ScheduledScan]:
        """Get all scheduled scans."""
        return list(self.scheduled_scans.values())
    
    def get_enabled_schedules(self) -> List[ScheduledScan]:
        """Get all enabled scheduled scans."""
        return [s for s in self.scheduled_scans.values() if s.enabled]
    
    def get_next_run_time(self, schedule_id: str) -> Optional[datetime]:
        """Get next run time for a schedule."""
        try:
            job = self.scheduler.get_job(schedule_id)
            return job.next_run_time if job else None
        except Exception as e:
            logger.error(f"Failed to get next run time: {e}")
            return None
    
    def get_execution_history(self, schedule_id: Optional[str] = None,
                            limit: int = 100) -> List[ScanExecution]:
        """
        Get execution history.
        
        Args:
            schedule_id: Filter by schedule ID (optional)
            limit: Maximum number of executions to return
            
        Returns:
            List of ScanExecution objects
        """
        history = self.execution_history
        
        if schedule_id:
            history = [e for e in history if e.schedule_id == schedule_id]
        
        # Return most recent first
        return sorted(history, key=lambda x: x.execution_time, reverse=True)[:limit]
    
    def _job_executed(self, event) -> None:
        """Handle successful job execution event."""
        logger.info(f"Job executed: {event.job_id}")
        
        # Update schedule run count
        if event.job_id in self.scheduled_scans:
            schedule = self.scheduled_scans[event.job_id]
            schedule.run_count += 1
            schedule.last_run = datetime.now()
            schedule.last_status = "completed"
    
    def _job_error(self, event) -> None:
        """Handle job error event."""
        logger.error(f"Job error: {event.job_id} - {event.exception}")
        
        # Update schedule status
        if event.job_id in self.scheduled_scans:
            schedule = self.scheduled_scans[event.job_id]
            schedule.last_run = datetime.now()
            schedule.last_status = "failed"
    
    def _save_schedule_to_db(self, schedule: ScheduledScan) -> None:
        """Save schedule to database for persistence."""
        # This would save to a scheduled_scans table
        # Implementation depends on database schema
        pass
    
    def _delete_schedule_from_db(self, schedule_id: str) -> None:
        """Delete schedule from database."""
        # This would delete from a scheduled_scans table
        # Implementation depends on database schema
        pass
    
    def record_execution(self, execution: ScanExecution) -> None:
        """Record a scan execution."""
        self.execution_history.append(execution)
        
        # Save to database
        # Implementation depends on database schema
        pass
    
    def get_statistics(self) -> Dict[str, Any]:
        """Get scheduler statistics."""
        jobs = self.scheduler.get_jobs()
        
        return {
            "total_schedules": len(self.scheduled_scans),
            "enabled_schedules": len(self.get_enabled_schedules()),
            "total_executions": sum(s.run_count for s in self.scheduled_scans.values()),
            "failed_executions": sum(1 for e in self.execution_history if e.status == "failed"),
            "successful_executions": sum(1 for e in self.execution_history if e.status == "completed"),
            "scheduler_running": self.scheduler.running,
            "active_jobs": len(jobs),
        }


# Singleton instance
_scheduler_instance: Optional[ScanScheduler] = None


def get_scheduler() -> Optional[ScanScheduler]:
    """Get singleton scan scheduler instance."""
    global _scheduler_instance
    if _scheduler_instance is None and APSCHEDULER_AVAILABLE:
        _scheduler_instance = ScanScheduler()
    return _scheduler_instance
