"""
Analytics Module - Charts, trends, and statistical analysis.
Provides visualization and insight into mining detection data.
"""

import logging
from typing import List, Dict, Optional, Any, Tuple
from dataclasses import dataclass
from datetime import datetime, timedelta
from collections import Counter, defaultdict
import json

try:
    import matplotlib.pyplot as plt
    import matplotlib.dates as mdates
    from matplotlib.figure import Figure
    MATPLOTLIB_AVAILABLE = True
except ImportError:
    MATPLOTLIB_AVAILABLE = False
    logging.warning("Matplotlib not available. Install with: pip install matplotlib")

try:
    import plotly.graph_objects as go
    import plotly.express as px
    from plotly.subplots import make_subplots
    PLOTLY_AVAILABLE = True
except ImportError:
    PLOTLY_AVAILABLE = False
    logging.warning("Plotly not available. Install with: pip install plotly")

try:
    import pandas as pd
    PANDAS_AVAILABLE = True
except ImportError:
    PANDAS_AVAILABLE = False
    logging.warning("Pandas not available. Install with: pip install pandas")

from .database import get_db_manager
from .config_manager import get_config_manager

logger = logging.getLogger(__name__)


@dataclass
class TimeSeriesData:
    """Time series data point."""
    timestamp: datetime
    value: float
    label: str = ""


@dataclass
class AnalyticsData:
    """Container for analytics data."""
    total_scans: int
    total_hosts: int
    total_miners: int
    miners_by_province: Dict[str, int]
    miners_by_isp: Dict[str, int]
    miners_by_type: Dict[str, int]
    scan_trends: List[TimeSeriesData]
    detection_rate_trends: List[TimeSeriesData]
    confidence_distribution: Dict[str, int]
    top_vulnerable_regions: List[Tuple[str, int]]
    high_risk_isps: List[Tuple[str, int]]


class AnalyticsService:
    """
    Analytics service for mining detection data.
    Generates charts, trends, and statistical reports.
    """
    
    def __init__(self):
        self.db = get_db_manager()
        self.config = get_config_manager().get()
        self._cached_data: Optional[AnalyticsData] = None
        self._cache_time: Optional[datetime] = None
        self._cache_ttl = timedelta(minutes=5)
    
    def get_analytics_data(self, force_refresh: bool = False) -> AnalyticsData:
        """
        Get current analytics data with caching.
        
        Args:
            force_refresh: Force refresh of cached data
            
        Returns:
            AnalyticsData object with current statistics
        """
        now = datetime.now()
        
        if (not force_refresh and 
            self._cached_data is not None and 
            self._cache_time is not None and
            now - self._cache_time < self._cache_ttl):
            return self._cached_data
        
        # Fetch fresh data
        stats = self.db.get_stats()
        scan_results = self.db.get_all_scan_results()
        
        # Process data
        miners_by_province = self._group_miners_by_province(scan_results)
        miners_by_isp = self._group_miners_by_isp(scan_results)
        miners_by_type = self._group_miners_by_type(scan_results)
        scan_trends = self._calculate_scan_trends(scan_results)
        detection_rate_trends = self._calculate_detection_rate_trends(scan_results)
        confidence_distribution = self._calculate_confidence_distribution(scan_results)
        top_vulnerable_regions = self._get_top_vulnerable_regions(scan_results, limit=10)
        high_risk_isps = self._get_high_risk_isps(scan_results, limit=10)
        
        analytics = AnalyticsData(
            total_scans=stats.get('total_scans', 0),
            total_hosts=stats.get('total_hosts', 0),
            total_miners=stats.get('total_miners', 0),
            miners_by_province=miners_by_province,
            miners_by_isp=miners_by_isp,
            miners_by_type=miners_by_type,
            scan_trends=scan_trends,
            detection_rate_trends=detection_rate_trends,
            confidence_distribution=confidence_distribution,
            top_vulnerable_regions=top_vulnerable_regions,
            high_risk_isps=high_risk_isps
        )
        
        self._cached_data = analytics
        self._cache_time = now
        
        return analytics
    
    def _group_miners_by_province(self, scan_results: List[Any]) -> Dict[str, int]:
        """Group miner detections by province."""
        counter = Counter()
        
        for result in scan_results:
            if hasattr(result, 'province') and result.province:
                counter[result.province] += 1
        
        return dict(counter.most_common())
    
    def _group_miners_by_isp(self, scan_results: List[Any]) -> Dict[str, int]:
        """Group miner detections by ISP."""
        counter = Counter()
        
        for result in scan_results:
            if hasattr(result, 'isp') and result.isp:
                counter[result.isp] += 1
        
        return dict(counter.most_common())
    
    def _group_miners_by_type(self, scan_results: List[Any]) -> Dict[str, int]:
        """Group miner detections by miner type."""
        counter = Counter()
        
        for result in scan_results:
            if hasattr(result, 'miner_type') and result.miner_type:
                counter[result.miner_type] += 1
        
        return dict(counter.most_common())
    
    def _calculate_scan_trends(self, scan_results: List[Any], 
                              days: int = 30) -> List[TimeSeriesData]:
        """Calculate daily scan trends."""
        if not scan_results:
            return []
        
        # Group by day
        daily_counts = defaultdict(int)
        
        for result in scan_results:
            if hasattr(result, 'start_time') and result.start_time:
                day_key = result.start_time.date()
                daily_counts[day_key] += 1
        
        # Generate time series
        end_date = datetime.now().date()
        start_date = end_date - timedelta(days=days)
        
        time_series = []
        current_date = start_date
        
        while current_date <= end_date:
            time_series.append(TimeSeriesData(
                timestamp=datetime.combine(current_date, datetime.min.time()),
                value=float(daily_counts.get(current_date, 0)),
                label=f"{current_date.strftime('%Y-%m-%d')}"
            ))
            current_date += timedelta(days=1)
        
        return time_series
    
    def _calculate_detection_rate_trends(self, scan_results: List[Any],
                                       days: int = 30) -> List[TimeSeriesData]:
        """Calculate daily detection rate trends."""
        if not scan_results:
            return []
        
        # Group by day: (scans, detections)
        daily_data = defaultdict(lambda: {'scans': 0, 'detections': 0})
        
        for result in scan_results:
            if hasattr(result, 'start_time') and result.start_time:
                day_key = result.start_time.date()
                daily_data[day_key]['scans'] += 1
                if hasattr(result, 'miners_detected') and result.miners_detected:
                    daily_data[day_key]['detections'] += result.miners_detected
        
        # Generate time series
        end_date = datetime.now().date()
        start_date = end_date - timedelta(days=days)
        
        time_series = []
        current_date = start_date
        
        while current_date <= end_date:
            data = daily_data.get(current_date, {'scans': 0, 'detections': 0})
            rate = (data['detections'] / data['scans'] * 100) if data['scans'] > 0 else 0.0
            
            time_series.append(TimeSeriesData(
                timestamp=datetime.combine(current_date, datetime.min.time()),
                value=rate,
                label=f"{current_date.strftime('%Y-%m-%d')}"
            ))
            current_date += timedelta(days=1)
        
        return time_series
    
    def _calculate_confidence_distribution(self, scan_results: List[Any]) -> Dict[str, int]:
        """Calculate distribution of confidence scores."""
        counter = Counter()
        
        for result in scan_results:
            if hasattr(result, 'confidence_score') and result.confidence_score is not None:
                score = result.confidence_score
                
                if score >= 0.8:
                    counter['High (80-100%)'] += 1
                elif score >= 0.5:
                    counter['Medium (50-79%)'] += 1
                elif score >= 0.2:
                    counter['Low (20-49%)'] += 1
                else:
                    counter['Very Low (0-19%)'] += 1
        
        return dict(counter)
    
    def _get_top_vulnerable_regions(self, scan_results: List[Any],
                                   limit: int = 10) -> List[Tuple[str, int]]:
        """Get top regions with most miner detections."""
        province_counts = self._group_miners_by_province(scan_results)
        return sorted(province_counts.items(), key=lambda x: x[1], reverse=True)[:limit]
    
    def _get_high_risk_isps(self, scan_results: List[Any],
                           limit: int = 10) -> List[Tuple[str, int]]:
        """Get ISPs with most miner detections."""
        isp_counts = self._group_miners_by_isp(scan_results)
        return sorted(isp_counts.items(), key=lambda x: x[1], reverse=True)[:limit]
    
    def generate_matplotlib_charts(self, output_dir: str = "reports/charts") -> Dict[str, str]:
        """
        Generate matplotlib charts and save to files.
        
        Args:
            output_dir: Directory to save chart files
            
        Returns:
            Dictionary mapping chart names to file paths
        """
        if not MATPLOTLIB_AVAILABLE:
            logger.warning("Matplotlib not available")
            return {}
        
        import os
        os.makedirs(output_dir, exist_ok=True)
        
        analytics = self.get_analytics_data()
        chart_paths = {}
        
        # Set style
        plt.style.use('seaborn-v0_8-darkgrid')
        
        # 1. Miner Distribution by Province (Bar Chart)
        if analytics.miners_by_province:
            fig, ax = plt.subplots(figsize=(12, 6))
            provinces = list(analytics.miners_by_province.keys())
            counts = list(analytics.miners_by_province.values())
            
            ax.barh(provinces, counts, color='steelblue')
            ax.set_xlabel('Number of Miners Detected')
            ax.set_ylabel('Province')
            ax.set_title('Miner Distribution by Province')
            ax.invert_yaxis()
            
            plt.tight_layout()
            path = os.path.join(output_dir, 'miners_by_province.png')
            plt.savefig(path, dpi=150, bbox_inches='tight')
            plt.close()
            chart_paths['miners_by_province'] = path
        
        # 2. Scan Trends Over Time (Line Chart)
        if analytics.scan_trends:
            fig, ax = plt.subplots(figsize=(12, 6))
            
            timestamps = [ts.timestamp for ts in analytics.scan_trends]
            values = [ts.value for ts in analytics.scan_trends]
            
            ax.plot(timestamps, values, marker='o', linewidth=2, color='darkorange')
            ax.set_xlabel('Date')
            ax.set_ylabel('Number of Scans')
            ax.set_title('Scan Trends Over Time')
            ax.xaxis.set_major_formatter(mdates.DateFormatter('%Y-%m-%d'))
            plt.xticks(rotation=45)
            
            plt.tight_layout()
            path = os.path.join(output_dir, 'scan_trends.png')
            plt.savefig(path, dpi=150, bbox_inches='tight')
            plt.close()
            chart_paths['scan_trends'] = path
        
        # 3. Detection Rate Trends (Line Chart)
        if analytics.detection_rate_trends:
            fig, ax = plt.subplots(figsize=(12, 6))
            
            timestamps = [ts.timestamp for ts in analytics.detection_rate_trends]
            values = [ts.value for ts in analytics.detection_rate_trends]
            
            ax.plot(timestamps, values, marker='s', linewidth=2, color='crimson')
            ax.set_xlabel('Date')
            ax.set_ylabel('Detection Rate (%)')
            ax.set_title('Detection Rate Trends Over Time')
            ax.xaxis.set_major_formatter(mdates.DateFormatter('%Y-%m-%d'))
            plt.xticks(rotation=45)
            
            plt.tight_layout()
            path = os.path.join(output_dir, 'detection_rate_trends.png')
            plt.savefig(path, dpi=150, bbox_inches='tight')
            plt.close()
            chart_paths['detection_rate_trends'] = path
        
        # 4. Miner Types (Pie Chart)
        if analytics.miners_by_type:
            fig, ax = plt.subplots(figsize=(10, 8))
            
            labels = list(analytics.miners_by_type.keys())
            sizes = list(analytics.miners_by_type.values())
            colors = plt.cm.Set3(range(len(labels)))
            
            ax.pie(sizes, labels=labels, autopct='%1.1f%%', colors=colors, startangle=90)
            ax.set_title('Miner Types Distribution')
            ax.axis('equal')
            
            plt.tight_layout()
            path = os.path.join(output_dir, 'miner_types.png')
            plt.savefig(path, dpi=150, bbox_inches='tight')
            plt.close()
            chart_paths['miner_types'] = path
        
        # 5. Confidence Score Distribution (Bar Chart)
        if analytics.confidence_distribution:
            fig, ax = plt.subplots(figsize=(10, 6))
            
            categories = list(analytics.confidence_distribution.keys())
            counts = list(analytics.confidence_distribution.values())
            
            ax.bar(categories, counts, color=['#d62728', '#ff7f0e', '#2ca02c', '#1f77b4'])
            ax.set_xlabel('Confidence Level')
            ax.set_ylabel('Count')
            ax.set_title('Confidence Score Distribution')
            plt.xticks(rotation=45)
            
            plt.tight_layout()
            path = os.path.join(output_dir, 'confidence_distribution.png')
            plt.savefig(path, dpi=150, bbox_inches='tight')
            plt.close()
            chart_paths['confidence_distribution'] = path
        
        logger.info(f"Generated {len(chart_paths)} matplotlib charts")
        return chart_paths
    
    def generate_plotly_charts(self, output_dir: str = "reports/charts") -> Dict[str, str]:
        """
        Generate interactive Plotly charts and save to files.
        
        Args:
            output_dir: Directory to save chart files
            
        Returns:
            Dictionary mapping chart names to file paths
        """
        if not PLOTLY_AVAILABLE:
            logger.warning("Plotly not available")
            return {}
        
        import os
        os.makedirs(output_dir, exist_ok=True)
        
        analytics = self.get_analytics_data()
        chart_paths = {}
        
        # 1. Miner Distribution by Province (Interactive Bar Chart)
        if analytics.miners_by_province:
            fig = go.Figure(data=[
                go.Bar(
                    x=list(analytics.miners_by_province.values()),
                    y=list(analytics.miners_by_province.keys()),
                    orientation='h',
                    marker_color='steelblue'
                )
            ])
            fig.update_layout(
                title='Miner Distribution by Province',
                xaxis_title='Number of Miners Detected',
                yaxis_title='Province',
                height=600
            )
            
            path = os.path.join(output_dir, 'miners_by_province_interactive.html')
            fig.write_html(path)
            chart_paths['miners_by_province_interactive'] = path
        
        # 2. Scan Trends (Interactive Line Chart)
        if analytics.scan_trends:
            fig = go.Figure()
            fig.add_trace(go.Scatter(
                x=[ts.timestamp for ts in analytics.scan_trends],
                y=[ts.value for ts in analytics.scan_trends],
                mode='lines+markers',
                name='Scans',
                line=dict(color='darkorange', width=2)
            ))
            fig.update_layout(
                title='Scan Trends Over Time',
                xaxis_title='Date',
                yaxis_title='Number of Scans',
                hovermode='x unified'
            )
            
            path = os.path.join(output_dir, 'scan_trends_interactive.html')
            fig.write_html(path)
            chart_paths['scan_trends_interactive'] = path
        
        # 3. Detection Rate Trends (Interactive Line Chart)
        if analytics.detection_rate_trends:
            fig = go.Figure()
            fig.add_trace(go.Scatter(
                x=[ts.timestamp for ts in analytics.detection_rate_trends],
                y=[ts.value for ts in analytics.detection_rate_trends],
                mode='lines+markers',
                name='Detection Rate',
                line=dict(color='crimson', width=2),
                fill='tozeroy'
            ))
            fig.update_layout(
                title='Detection Rate Trends Over Time',
                xaxis_title='Date',
                yaxis_title='Detection Rate (%)',
                hovermode='x unified'
            )
            
            path = os.path.join(output_dir, 'detection_rate_trends_interactive.html')
            fig.write_html(path)
            chart_paths['detection_rate_trends_interactive'] = path
        
        # 4. Miner Types (Interactive Pie Chart)
        if analytics.miners_by_type:
            fig = go.Figure(data=[go.Pie(
                labels=list(analytics.miners_by_type.keys()),
                values=list(analytics.miners_by_type.values()),
                hole=0.3
            )])
            fig.update_layout(
                title='Miner Types Distribution',
                height=600
            )
            
            path = os.path.join(output_dir, 'miner_types_interactive.html')
            fig.write_html(path)
            chart_paths['miner_types_interactive'] = path
        
        logger.info(f"Generated {len(chart_paths)} Plotly charts")
        return chart_paths
    
    def generate_summary_report(self) -> Dict[str, Any]:
        """Generate text summary of analytics data."""
        analytics = self.get_analytics_data()
        
        # Calculate overall detection rate
        detection_rate = 0.0
        if analytics.total_hosts > 0:
            detection_rate = (analytics.total_miners / analytics.total_hosts) * 100
        
        # Get top province
        top_province = analytics.top_vulnerable_regions[0] if analytics.top_vulnerable_regions else ("N/A", 0)
        
        # Get top ISP
        top_isp = analytics.high_risk_isps[0] if analytics.high_risk_isps else ("N/A", 0)
        
        # Get most common miner type
        top_miner_type = max(analytics.miners_by_type.items(), 
                           key=lambda x: x[1]) if analytics.miners_by_type else ("N/A", 0)
        
        return {
            "summary": {
                "total_scans": analytics.total_scans,
                "total_hosts": analytics.total_hosts,
                "total_miners": analytics.total_miners,
                "detection_rate_percent": round(detection_rate, 2),
            },
            "top_vulnerable_province": {
                "name": top_province[0],
                "miners_detected": top_province[1]
            },
            "top_risk_isp": {
                "name": top_isp[0],
                "miners_detected": top_isp[1]
            },
            "most_common_miner_type": {
                "type": top_miner_type[0],
                "count": top_miner_type[1]
            },
            "miners_by_province": analytics.miners_by_province,
            "miners_by_isp": analytics.miners_by_isp,
            "miners_by_type": analytics.miners_by_type,
            "confidence_distribution": analytics.confidence_distribution,
            "generated_at": datetime.now().isoformat()
        }
    
    def export_analytics_json(self, output_path: str = "reports/analytics.json") -> str:
        """Export analytics data to JSON file."""
        summary = self.generate_summary_report()
        
        import os
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(summary, f, indent=2, ensure_ascii=False)
        
        logger.info(f"Exported analytics to {output_path}")
        return output_path
    
    def clear_cache(self):
        """Clear analytics cache."""
        self._cached_data = None
        self._cache_time = None


# Singleton instance
_analytics_service_instance: Optional[AnalyticsService] = None


def get_analytics_service() -> AnalyticsService:
    """Get singleton analytics service instance."""
    global _analytics_service_instance
    if _analytics_service_instance is None:
        _analytics_service_instance = AnalyticsService()
    return _analytics_service_instance
