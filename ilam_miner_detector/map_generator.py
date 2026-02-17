"""
Map generation module for Ilam Miner Detector.
Creates interactive Folium maps with detection markers and heatmaps.
"""

import folium
from folium.plugins import HeatMap, MarkerCluster
from typing import List, Dict, Optional, Tuple, Any
from dataclasses import dataclass
from pathlib import Path
import json
import logging

from .geolocation import GeolocationResult
from .database import HostRecord

logger = logging.getLogger(__name__)


@dataclass
class MapMarker:
    """A marker for the map."""
    latitude: float
    longitude: float
    title: str
    popup_content: str
    color: str = "red"
    icon: str = "warning"


class MapGenerator:
    """
    Generates interactive Folium maps for scan results.
    """
    
    # Ilam province center coordinates
    ILAM_CENTER = [33.0, 46.5]
    DEFAULT_ZOOM = 10
    
    # Marker colors by confidence
    CONFIDENCE_COLORS = {
        "high": "red",      # 80-100%
        "medium": "orange", # 50-79%
        "low": "yellow",    # 20-49%
        "none": "blue"      # <20%
    }
    
    def __init__(self):
        self.map_instance: Optional[folium.Map] = None
        
    def create_map(self, center: Optional[List[float]] = None,
                   zoom: int = DEFAULT_ZOOM) -> folium.Map:
        """
        Create a new Folium map centered on Ilam province.
        
        Args:
            center: [lat, lon] center coordinates (default: Ilam center)
            zoom: Initial zoom level
            
        Returns:
            Folium Map object
        """
        center = center or self.ILAM_CENTER
        
        self.map_instance = folium.Map(
            location=center,
            zoom_start=zoom,
            tiles='CartoDB positron'
        )
        
        # Add tile layers
        folium.TileLayer(
            'OpenStreetMap',
            name='Street Map',
            control=True
        ).add_to(self.map_instance)
        
        folium.TileLayer(
            'CartoDB dark_matter',
            name='Dark Mode',
            control=True
        ).add_to(self.map_instance)
        
        # Add layer control
        folium.LayerControl().add_to(self.map_instance)
        
        return self.map_instance
    
    def add_ilam_boundary(self, map_obj: Optional[folium.Map] = None) -> folium.Map:
        """
        Add Ilam province boundary polygon to map.
        
        Args:
            map_obj: Folium map (uses instance map if None)
            
        Returns:
            Folium Map with boundary
        """
        m = map_obj or self.map_instance
        if m is None:
            m = self.create_map()
        
        # Approximate Ilam province boundary (simplified)
        ilam_boundary = [
            [32.5, 46.0],
            [32.5, 47.5],
            [33.5, 47.5],
            [33.5, 46.0],
            [32.5, 46.0]
        ]
        
        folium.Polygon(
            locations=ilam_boundary,
            popup='Ilam Province',
            color='blue',
            weight=2,
            fill=True,
            fill_color='blue',
            fill_opacity=0.1
        ).add_to(m)
        
        return m
    
    def add_markers(self, markers: List[MapMarker],
                   cluster: bool = True,
                   map_obj: Optional[folium.Map] = None) -> folium.Map:
        """
        Add markers to the map.
        
        Args:
            markers: List of MapMarker objects
            cluster: Whether to use marker clustering
            map_obj: Folium map (uses instance map if None)
            
        Returns:
            Folium Map with markers
        """
        m = map_obj or self.map_instance
        if m is None:
            m = self.create_map()
        
        if cluster:
            marker_cluster = MarkerCluster(name='Detections').add_to(m)
            target = marker_cluster
        else:
            target = m
        
        for marker in markers:
            folium.Marker(
                location=[marker.latitude, marker.longitude],
                popup=folium.Popup(marker.popup_content, max_width=300),
                tooltip=marker.title,
                icon=folium.Icon(color=marker.color, icon=marker.icon, prefix='fa')
            ).add_to(target)
        
        return m
    
    def add_heatmap(self, points: List[Tuple[float, float]],
                   map_obj: Optional[folium.Map] = None) -> folium.Map:
        """
        Add a heatmap layer to the map.
        
        Args:
            points: List of (lat, lon) tuples
            map_obj: Folium map (uses instance map if None)
            
        Returns:
            Folium Map with heatmap
        """
        m = map_obj or self.map_instance
        if m is None:
            m = self.create_map()
        
        if points:
            HeatMap(
                data=points,
                name='Detection Heatmap',
                min_opacity=0.3,
                radius=15,
                blur=25,
                max_zoom=10
            ).add_to(m)
        
        return m
    
    def add_scan_results(self, results: List[Dict[str, Any]],
                        include_heatmap: bool = True,
                        map_obj: Optional[folium.Map] = None) -> folium.Map:
        """
        Add scan results to map with geolocation data.
        
        Args:
            results: List of dicts with ip, lat, lon, confidence, etc.
            include_heatmap: Whether to add heatmap layer
            map_obj: Folium map (uses instance map if None)
            
        Returns:
            Folium Map with results
        """
        m = map_obj or self.map_instance
        if m is None:
            m = self.create_map()
        
        markers = []
        heatmap_points = []
        
        for result in results:
            lat = result.get('latitude', 0)
            lon = result.get('longitude', 0)
            
            if lat == 0 and lon == 0:
                continue
            
            confidence = result.get('confidence_score', 0)
            color = self._get_color_by_confidence(confidence)
            
            # Create popup content
            popup = self._create_popup_content(result)
            
            marker = MapMarker(
                latitude=lat,
                longitude=lon,
                title=f"{result.get('ip_address', 'Unknown')} ({confidence:.0f}%)",
                popup_content=popup,
                color=color,
                icon='exclamation-triangle' if confidence >= 50 else 'info-circle'
            )
            markers.append(marker)
            heatmap_points.append([lat, lon])
        
        # Add markers
        self.add_markers(markers, cluster=True, map_obj=m)
        
        # Add heatmap if requested
        if include_heatmap and heatmap_points:
            self.add_heatmap(heatmap_points, map_obj=m)
        
        return m
    
    def save_map(self, output_path: str, map_obj: Optional[folium.Map] = None) -> str:
        """
        Save map to HTML file.
        
        Args:
            output_path: Path to save HTML file
            map_obj: Folium map (uses instance map if None)
            
        Returns:
            Path to saved file
        """
        m = map_obj or self.map_instance
        if m is None:
            m = self.create_map()
        
        Path(output_path).parent.mkdir(parents=True, exist_ok=True)
        m.save(output_path)
        logger.info(f"Map saved to {output_path}")
        return output_path
    
    def _get_color_by_confidence(self, confidence: float) -> str:
        """Get marker color based on confidence score."""
        if confidence >= 80:
            return self.CONFIDENCE_COLORS["high"]
        elif confidence >= 50:
            return self.CONFIDENCE_COLORS["medium"]
        elif confidence >= 20:
            return self.CONFIDENCE_COLORS["low"]
        return self.CONFIDENCE_COLORS["none"]
    
    def _create_popup_content(self, result: Dict[str, Any]) -> str:
        """Create HTML popup content for a result."""
        ip = result.get('ip_address', 'Unknown')
        confidence = result.get('confidence_score', 0)
        miner_type = result.get('miner_type', 'Unknown')
        city = result.get('city', 'Unknown')
        region = result.get('region', 'Unknown')
        country = result.get('country', 'Unknown')
        isp = result.get('isp', 'Unknown')
        open_ports = result.get('open_ports', [])
        
        ports_str = ', '.join(str(p) for p in open_ports[:5])
        if len(open_ports) > 5:
            ports_str += f" (+{len(open_ports) - 5} more)"
        
        html = f"""
        <div style="min-width: 200px;">
            <h4 style="margin: 0 0 10px 0; color: #d32f2f;">
                <i class="fa fa-exclamation-circle"></i> Potential Miner Detected
            </h4>
            <table style="width: 100%; font-size: 12px;">
                <tr><td><b>IP:</b></td><td>{ip}</td></tr>
                <tr><td><b>Confidence:</b></td><td><span style="color: {'red' if confidence >= 80 else 'orange' if confidence >= 50 else 'yellow'}">{confidence:.1f}%</span></td></tr>
                <tr><td><b>Type:</b></td><td>{miner_type}</td></tr>
                <tr><td><b>Location:</b></td><td>{city}, {region}, {country}</td></tr>
                <tr><td><b>ISP:</b></td><td>{isp}</td></tr>
                <tr><td><b>Open Ports:</b></td><td>{ports_str or 'None'}</td></tr>
            </table>
        </div>
        """
        return html
    
    def create_summary_map(self, scan_results: List[Dict],
                          output_path: str,
                          title: str = "Ilam Miner Detection Results") -> str:
        """
        Create a complete summary map with all features.
        
        Args:
            scan_results: List of scan result dictionaries
            output_path: Path to save HTML
            title: Map title
            
        Returns:
            Path to saved map
        """
        # Create base map
        m = self.create_map()
        
        # Add title
        title_html = f'''
            <div style="position: fixed; 
                        top: 10px; left: 50px; width: 400px;
                        background-color: white; 
                        border: 2px solid #333;
                        border-radius: 5px;
                        padding: 10px;
                        z-index: 9999;
                        font-family: Arial;">
                <h3 style="margin: 0; color: #333;">{title}</h3>
                <p style="margin: 5px 0 0 0; font-size: 12px;">
                    Total Detections: {len(scan_results)}
                </p>
            </div>
        '''
        m.get_root().html.add_child(folium.Element(title_html))
        
        # Add Ilam boundary
        self.add_ilam_boundary(m)
        
        # Add scan results
        self.add_scan_results(scan_results, include_heatmap=True, map_obj=m)
        
        # Add legend
        legend_html = '''
        <div style="position: fixed; 
                    bottom: 50px; right: 50px; 
                    background-color: white; 
                    border: 2px solid #333;
                    border-radius: 5px;
                    padding: 10px;
                    z-index: 9999;
                    font-family: Arial;
                    font-size: 12px;">
            <h4 style="margin: 0 0 10px 0;">Confidence Levels</h4>
            <div><span style="background-color: red; padding: 2px 8px;">&nbsp;</span> High (80-100%)</div>
            <div><span style="background-color: orange; padding: 2px 8px;">&nbsp;</span> Medium (50-79%)</div>
            <div><span style="background-color: yellow; padding: 2px 8px;">&nbsp;</span> Low (20-49%)</div>
            <div><span style="background-color: blue; padding: 2px 8px;">&nbsp;</span> None (&lt;20%)</div>
        </div>
        '''
        m.get_root().html.add_child(folium.Element(legend_html))
        
        return self.save_map(output_path, m)


def get_map_generator() -> MapGenerator:
    """Get map generator instance."""
    return MapGenerator()
