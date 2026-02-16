"""
Map generation utilities using Folium for visualizing miner locations.
"""

import folium
from folium.plugins import MarkerCluster, HeatMap
from typing import List, Dict, Any
import logging


class MapGenerator:
    """Generates interactive HTML maps with miner locations."""
    
    # Ilam province center coordinates
    ILAM_CENTER = [33.0, 46.5]
    ILAM_BOUNDS = [
        [32.5, 46.0],  # Southwest corner
        [33.5, 47.5]   # Northeast corner
    ]
    
    def __init__(self):
        """Initialize map generator."""
        self.logger = logging.getLogger(__name__)
    
    def create_map(self, 
                   hosts: List[Dict[str, Any]],
                   center: List[float] = None,
                   zoom_start: int = 10,
                   show_heatmap: bool = True,
                   show_clusters: bool = True) -> folium.Map:
        """
        Create an interactive map with miner locations.
        
        Args:
            hosts: List of host dictionaries with geolocation data
            center: Map center coordinates [lat, lon]. Defaults to Ilam center
            zoom_start: Initial zoom level
            show_heatmap: Show heatmap layer
            show_clusters: Use marker clustering
            
        Returns:
            Folium Map object
        """
        if center is None:
            center = self.ILAM_CENTER
        
        # Create base map
        m = folium.Map(
            location=center,
            zoom_start=zoom_start,
            tiles='OpenStreetMap'
        )
        
        # Add Ilam province boundary rectangle
        folium.Rectangle(
            bounds=self.ILAM_BOUNDS,
            color='red',
            fill=False,
            weight=2,
            popup='Ilam Province Boundary (Approximate)',
            tooltip='Ilam Province'
        ).add_to(m)
        
        # Prepare data for markers and heatmap
        marker_data = []
        heatmap_data = []
        
        for host in hosts:
            lat = host.get('latitude')
            lon = host.get('longitude')
            
            if lat is None or lon is None:
                continue
            
            ip = host.get('ip_address', 'Unknown')
            city = host.get('city', 'Unknown')
            region = host.get('region', 'Unknown')
            miner_type = host.get('miner_type', 'Unknown')
            open_ports = host.get('open_ports', '[]')
            
            marker_data.append({
                'lat': lat,
                'lon': lon,
                'ip': ip,
                'city': city,
                'region': region,
                'miner_type': miner_type,
                'ports': open_ports
            })
            
            heatmap_data.append([lat, lon])
        
        self.logger.info(f"Generating map with {len(marker_data)} markers")
        
        # Add markers
        if show_clusters and len(marker_data) > 10:
            marker_cluster = MarkerCluster().add_to(m)
            parent = marker_cluster
        else:
            parent = m
        
        for data in marker_data:
            # Color code by miner type
            color = self._get_marker_color(data['miner_type'])
            
            # Create popup HTML
            popup_html = f"""
            <div style="font-family: Arial; width: 200px;">
                <h4 style="margin: 0; color: {color};">🔍 Miner Detected</h4>
                <hr style="margin: 5px 0;">
                <b>IP:</b> {data['ip']}<br>
                <b>Location:</b> {data['city']}, {data['region']}<br>
                <b>Type:</b> {data['miner_type']}<br>
                <b>Ports:</b> {data['ports']}<br>
                <b>Coordinates:</b> {data['lat']:.4f}, {data['lon']:.4f}
            </div>
            """
            
            folium.Marker(
                location=[data['lat'], data['lon']],
                popup=folium.Popup(popup_html, max_width=300),
                tooltip=f"{data['ip']} - {data['miner_type']}",
                icon=folium.Icon(color=color, icon='info-sign')
            ).add_to(parent)
        
        # Add heatmap layer
        if show_heatmap and heatmap_data:
            HeatMap(
                heatmap_data,
                name='Miner Density Heatmap',
                radius=15,
                blur=25,
                max_zoom=13,
                gradient={0.4: 'blue', 0.6: 'yellow', 0.8: 'orange', 1.0: 'red'}
            ).add_to(m)
        
        # Add layer control
        folium.LayerControl().add_to(m)
        
        # Add legend
        legend_html = '''
        <div style="position: fixed; 
                    bottom: 50px; right: 50px; width: 180px; height: auto; 
                    background-color: white; border:2px solid grey; z-index:9999; 
                    font-size:14px; padding: 10px">
        <p style="margin: 0; font-weight: bold;">Miner Types</p>
        <p style="margin: 5px 0;"><i class="fa fa-map-marker" style="color:red"></i> Stratum</p>
        <p style="margin: 5px 0;"><i class="fa fa-map-marker" style="color:orange"></i> Bitcoin</p>
        <p style="margin: 5px 0;"><i class="fa fa-map-marker" style="color:blue"></i> Ethereum</p>
        <p style="margin: 5px 0;"><i class="fa fa-map-marker" style="color:purple"></i> Monero</p>
        <p style="margin: 5px 0;"><i class="fa fa-map-marker" style="color:gray"></i> Unknown</p>
        </div>
        '''
        m.get_root().html.add_child(folium.Element(legend_html))
        
        return m
    
    def _get_marker_color(self, miner_type: str) -> str:
        """
        Get marker color based on miner type.
        
        Args:
            miner_type: Type of miner
            
        Returns:
            Color name for Folium marker
        """
        colors = {
            'stratum': 'red',
            'bitcoin': 'orange',
            'ethereum': 'blue',
            'monero': 'purple',
        }
        
        return colors.get(miner_type.lower() if miner_type else '', 'gray')
    
    def save_map(self, m: folium.Map, filepath: str):
        """
        Save map to HTML file.
        
        Args:
            m: Folium Map object
            filepath: Output file path
        """
        m.save(filepath)
        self.logger.info(f"Map saved to {filepath}")
    
    def generate_and_save(self, 
                         hosts: List[Dict[str, Any]], 
                         filepath: str,
                         **kwargs) -> str:
        """
        Generate and save map in one call.
        
        Args:
            hosts: List of host dictionaries
            filepath: Output file path
            **kwargs: Additional arguments for create_map
            
        Returns:
            Path to saved map file
        """
        m = self.create_map(hosts, **kwargs)
        self.save_map(m, filepath)
        return filepath
