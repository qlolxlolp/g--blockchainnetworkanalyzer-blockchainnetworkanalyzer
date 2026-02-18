"""
Detection Rules Engine - YAML-based configuration for miner detection.
Flexible, extensible rule system for identifying cryptocurrency mining activity.
"""

import yaml
import logging
from typing import List, Dict, Optional, Any
from dataclasses import dataclass
from pathlib import Path

logger = logging.getLogger(__name__)


@dataclass
class DetectionRule:
    """Represents a detection rule."""
    name: str
    description: str
    enabled: bool
    priority: int  # Higher = more important
    conditions: Dict[str, Any]
    actions: List[str]
    confidence_score: float = 0.5
    tags: List[str] = None
    
    def __post_init__(self):
        if self.tags is None:
            self.tags = []


@dataclass
class RuleMatch:
    """Result of a rule match."""
    rule_name: str
    matched: bool
    confidence: float
    reason: str
    matched_conditions: Dict[str, Any]
    tags: List[str]


class DetectionRulesEngine:
    """
    YAML-based detection rules engine.
    Loads, validates, and executes detection rules.
    """
    
    DEFAULT_RULES_CONFIG = """
# Default Detection Rules for Cryptocurrency Mining Detection
# Format: YAML with rule definitions

version: "1.0"
rules:
  # Stratum Mining Pool Detection
  - name: "stratum_pool_detection"
    description: "Detect Stratum mining pool connections"
    enabled: true
    priority: 90
    confidence_score: 0.85
    tags: ["stratum", "mining_pool", "high_confidence"]
    conditions:
      ports:
        - 3333
        - 4444
        - 4028
        - 7777
        - 14433
        - 14444
      banner_patterns:
        - "mining.subscribe"
        - "mining.authorize"
        - "stratum"
        - '{"method": "mining'
      min_ports: 1
    actions:
      - "log"
      - "alert"
      - "database_record"
    
  # Bitcoin Node Detection
  - name: "bitcoin_node_detection"
    description: "Detect Bitcoin P2P nodes and RPC servers"
    enabled: true
    priority: 85
    confidence_score: 0.80
    tags: ["bitcoin", "p2p", "rpc"]
    conditions:
      ports:
        - 8332  # RPC
        - 8333  # P2P
      banner_patterns:
        - "bitcoin"
        - "bitcoind"
        - '{"result": null, "error": null'
      min_ports: 1
    actions:
      - "log"
      - "alert"
      - "database_record"
    
  # Ethereum Node Detection
  - name: "ethereum_node_detection"
    description: "Detect Ethereum nodes"
    enabled: true
    priority: 85
    confidence_score: 0.80
    tags: ["ethereum", "rpc", "p2p"]
    conditions:
      ports:
        - 8545  # RPC
        - 30303 # P2P
      banner_patterns:
        - "ethereum"
        - "geth"
        - "parity"
        - '{"jsonrpc":"2.0"'
      min_ports: 1
    actions:
      - "log"
      - "alert"
      - "database_record"
    
  # Monero Mining Detection
  - name: "monero_mining_detection"
    description: "Detect Monero mining operations"
    enabled: true
    priority: 80
    confidence_score: 0.75
    tags: ["monero", "xmrig", "cpu_mining"]
    conditions:
      ports:
        - 3333
        - 8080
        - 8081
      banner_patterns:
        - "xmrig"
        - "XMRig"
        - "monero"
        - "cryptonight"
      min_ports: 1
    actions:
      - "log"
      - "alert"
      - "database_record"
    
  # Multiple Mining Ports
  - name: "multiple_mining_ports"
    description: "Detect multiple mining-related ports open"
    enabled: true
    priority: 75
    confidence_score: 0.70
    tags: ["multi_port", "suspicious"]
    conditions:
      port_combinations:
        - [3333, 4444]
        - [8332, 8333]
        - [8545, 30303]
        - [3333, 8080]
        - [4028, 7777]
      min_combinations: 1
    actions:
      - "log"
      - "alert"
      - "database_record"
    
  # Web-based Mining Detection
  - name: "web_mining_detection"
    description: "Detect web-based cryptocurrency mining"
    enabled: true
    priority: 70
    confidence_score: 0.65
    tags: ["web_mining", "javascript", "coinhive"]
    conditions:
      ports:
        - 80
        - 443
        - 8080
      banner_patterns:
        - "coinhive"
        - "crypto-loot"
        - "jsecoin"
        - "cryptonight.wasm"
        - "monero.miner"
      min_ports: 1
    actions:
      - "log"
      - "alert"
      - "database_record"
    
  # High Bandwidth Usage Pattern
  - name: "high_bandwidth_pattern"
    description: "Detect high bandwidth usage patterns"
    enabled: true
    priority: 60
    confidence_score: 0.50
    tags: ["bandwidth", "pattern", "low_confidence"]
    conditions:
      bandwidth_threshold_mbps: 10
      duration_minutes: 5
    actions:
      - "log"
      - "database_record"
    
  # GPU Miner Pattern
  - name: "gpu_miner_pattern"
    description: "Detect GPU mining software patterns"
    enabled: true
    priority: 75
    confidence_score: 0.70
    tags: ["gpu", "mining", "cuda", "opencl"]
    conditions:
      banner_patterns:
        - "cl_device_type_gpu"
        - "cuda"
        - "opencl"
        - "ethminer"
        - "claymore"
        - " PhoenixMiner"
    actions:
      - "log"
      - "alert"
      - "database_record"
    
  # Low Confidence Suspicious Port
  - name: "suspicious_mining_port"
    description: "Low confidence detection of single mining port"
    enabled: true
    priority: 50
    confidence_score: 0.30
    tags: ["suspicious", "low_confidence", "single_port"]
    conditions:
      ports:
        - 3333
        - 4028
        - 8332
        - 8545
      min_ports: 1
      max_ports: 1
    actions:
      - "log"
      - "database_record"
"""
    
    def __init__(self, config_path: Optional[str] = None):
        self.config_path = config_path or "config/detection_rules.yaml"
        self.rules: List[DetectionRule] = []
        self._load_rules()
    
    def _load_rules(self) -> None:
        """Load rules from YAML configuration file."""
        config_path = Path(self.config_path)
        
        if config_path.exists():
            try:
                with open(config_path, 'r', encoding='utf-8') as f:
                    config = yaml.safe_load(f)
                self._parse_rules(config)
                logger.info(f"Loaded {len(self.rules)} detection rules from {self.config_path}")
            except Exception as e:
                logger.error(f"Failed to load rules from {self.config_path}: {e}")
                logger.info("Using default rules")
                self._load_default_rules()
        else:
            logger.info(f"Config file not found at {self.config_path}, using default rules")
            self._load_default_rules()
    
    def _load_default_rules(self) -> None:
        """Load default rules."""
        config = yaml.safe_load(self.DEFAULT_RULES_CONFIG)
        self._parse_rules(config)
    
    def _parse_rules(self, config: Dict[str, Any]) -> None:
        """Parse rules from configuration dictionary."""
        self.rules = []
        
        if "rules" not in config:
            logger.warning("No rules found in configuration")
            return
        
        for rule_data in config["rules"]:
            try:
                rule = DetectionRule(
                    name=rule_data.get("name", "unnamed_rule"),
                    description=rule_data.get("description", ""),
                    enabled=rule_data.get("enabled", True),
                    priority=rule_data.get("priority", 50),
                    conditions=rule_data.get("conditions", {}),
                    actions=rule_data.get("actions", ["log"]),
                    confidence_score=rule_data.get("confidence_score", 0.5),
                    tags=rule_data.get("tags", [])
                )
                self.rules.append(rule)
            except Exception as e:
                logger.error(f"Failed to parse rule: {e}")
    
    def evaluate(self, scan_result: Any) -> List[RuleMatch]:
        """
        Evaluate scan result against all enabled rules.
        
        Args:
            scan_result: Host scan result with ports and banners
            
        Returns:
            List of RuleMatch objects for matched rules
        """
        matches = []
        
        # Get scan result data
        open_ports = []
        banners = {}
        
        # Handle different scan result formats
        if hasattr(scan_result, 'open_ports'):
            open_ports = [p.port if hasattr(p, 'port') else p for p in scan_result.open_ports]
            banners = {p.port if hasattr(p, 'port') else p: p.banner if hasattr(p, 'banner') else "" 
                      for p in scan_result.open_ports}
        elif isinstance(scan_result, dict):
            open_ports = scan_result.get('open_ports', [])
            banners = scan_result.get('banners', {})
        
        # Sort rules by priority (highest first)
        sorted_rules = sorted([r for r in self.rules if r.enabled], 
                              key=lambda r: r.priority, reverse=True)
        
        for rule in sorted_rules:
            match = self._evaluate_rule(rule, open_ports, banners, scan_result)
            if match.matched:
                matches.append(match)
        
        return matches
    
    def _evaluate_rule(self, rule: DetectionRule, open_ports: List[int], 
                      banners: Dict[int, str], scan_result: Any) -> RuleMatch:
        """
        Evaluate a single rule against scan result.
        
        Args:
            rule: Detection rule to evaluate
            open_ports: List of open ports
            banners: Dictionary of port -> banner
            scan_result: Full scan result
            
        Returns:
            RuleMatch object
        """
        matched = False
        matched_conditions = {}
        reasons = []
        
        conditions = rule.conditions
        
        # Check port conditions
        if 'ports' in conditions:
            required_ports = set(conditions['ports'])
            open_ports_set = set(open_ports)
            
            min_ports = conditions.get('min_ports', 1)
            max_ports = conditions.get('max_ports', len(required_ports))
            
            matched_ports = required_ports & open_ports_set
            ports_match = min_ports <= len(matched_ports) <= max_ports
            
            if ports_match:
                matched_conditions['ports'] = list(matched_ports)
                matched = True
                reasons.append(f"Matched {len(matched_ports)} required ports")
        
        # Check banner pattern conditions
        if 'banner_patterns' in conditions:
            patterns = conditions['banner_patterns']
            banner_matches = []
            
            for port, banner in banners.items():
                banner_str = str(banner).lower()
                for pattern in patterns:
                    if pattern.lower() in banner_str:
                        banner_matches.append((port, pattern))
            
            if banner_matches:
                matched_conditions['banner_patterns'] = banner_matches
                matched = True
                reasons.append(f"Matched {len(banner_matches)} banner patterns")
        
        # Check port combination conditions
        if 'port_combinations' in conditions:
            combinations = conditions['port_combinations']
            min_combinations = conditions.get('min_combinations', 1)
            open_ports_set = set(open_ports)
            
            matched_combinations = []
            for combo in combinations:
                combo_set = set(combo)
                if combo_set.issubset(open_ports_set):
                    matched_combinations.append(combo)
            
            if len(matched_combinations) >= min_combinations:
                matched_conditions['port_combinations'] = matched_combinations
                matched = True
                reasons.append(f"Matched {len(matched_combinations)} port combinations")
        
        # Check bandwidth conditions (if available)
        if 'bandwidth_threshold_mbps' in conditions:
            # This would need actual bandwidth measurement
            # For now, we skip this check
            pass
        
        return RuleMatch(
            rule_name=rule.name,
            matched=matched,
            confidence=rule.confidence_score if matched else 0.0,
            reason="; ".join(reasons),
            matched_conditions=matched_conditions,
            tags=rule.tags
        )
    
    def calculate_overall_confidence(self, matches: List[RuleMatch]) -> float:
        """
        Calculate overall confidence score from rule matches.
        
        Args:
            matches: List of RuleMatch objects
            
        Returns:
            Overall confidence score (0.0 to 1.0)
        """
        if not matches:
            return 0.0
        
        # Weighted average based on priority
        total_weight = 0
        weighted_sum = 0
        
        for match in matches:
            weight = match.confidence  # Use confidence as weight
            total_weight += weight
            weighted_sum += weight * match.confidence
        
        if total_weight == 0:
            return 0.0
        
        overall = weighted_sum / total_weight
        
        # Cap at 1.0
        return min(overall, 1.0)
    
    def get_rules_by_tag(self, tag: str) -> List[DetectionRule]:
        """Get all rules with a specific tag."""
        return [rule for rule in self.rules if tag in rule.tags]
    
    def get_enabled_rules(self) -> List[DetectionRule]:
        """Get all enabled rules."""
        return [rule for rule in self.rules if rule.enabled]
    
    def disable_rule(self, rule_name: str) -> bool:
        """Disable a rule by name."""
        for rule in self.rules:
            if rule.name == rule_name:
                rule.enabled = False
                return True
        return False
    
    def enable_rule(self, rule_name: str) -> bool:
        """Enable a rule by name."""
        for rule in self.rules:
            if rule.name == rule_name:
                rule.enabled = True
                return True
        return False
    
    def save_rules(self, output_path: Optional[str] = None) -> None:
        """Save current rules to YAML file."""
        output_path = output_path or self.config_path
        
        config = {
            "version": "1.0",
            "rules": []
        }
        
        for rule in self.rules:
            rule_dict = {
                "name": rule.name,
                "description": rule.description,
                "enabled": rule.enabled,
                "priority": rule.priority,
                "confidence_score": rule.confidence_score,
                "tags": rule.tags,
                "conditions": rule.conditions,
                "actions": rule.actions
            }
            config["rules"].append(rule_dict)
        
        Path(output_path).parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_path, 'w', encoding='utf-8') as f:
            yaml.dump(config, f, default_flow_style=False, allow_unicode=True)
        
        logger.info(f"Saved {len(self.rules)} rules to {output_path}")
    
    def import_rules(self, import_path: str) -> None:
        """Import rules from YAML file."""
        import_path = Path(import_path)
        
        if not import_path.exists():
            raise FileNotFoundError(f"Rules file not found: {import_path}")
        
        with open(import_path, 'r', encoding='utf-8') as f:
            config = yaml.safe_load(f)
        
        self._parse_rules(config)
        logger.info(f"Imported rules from {import_path}")


# Singleton instance
_rules_engine_instance: Optional[DetectionRulesEngine] = None


def get_detection_rules_engine() -> DetectionRulesEngine:
    """Get singleton detection rules engine instance."""
    global _rules_engine_instance
    if _rules_engine_instance is None:
        _rules_engine_instance = DetectionRulesEngine()
    return _rules_engine_instance
