
import React, { useEffect, useRef, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { MapPin, Navigation, Radar, Route, Zap } from 'lucide-react';

interface DetectedDevice {
  ip: string;
  location: {
    lat: number;
    lng: number;
    city: string;
    province: string;
    country?: string;
    isp?: string;
  };
  ports: number[];
  minerType: string;
  confidence: number;
  timestamp: string;
}

const SmartMap = () => {
  const mapRef = useRef<HTMLDivElement>(null);
  const [map, setMap] = useState<google.maps.Map | null>(null);
  const [isLoaded, setIsLoaded] = useState(false);
  const [userLocation, setUserLocation] = useState<{lat: number, lng: number} | null>(null);
  const [detectedDevices, setDetectedDevices] = useState<DetectedDevice[]>([]);
  const [markers, setMarkers] = useState<google.maps.Marker[]>([]);
  const [directionsRenderer, setDirectionsRenderer] = useState<google.maps.DirectionsRenderer | null>(null);

  // Load detected miners from localStorage
  useEffect(() => {
    const loadDetectedDevices = () => {
      try {
        // Load results from different scan types
        const ilamResults = localStorage.getItem('ilamMinerResults');
        const customResults = localStorage.getItem('minerDetectionResults');
        const selectedDevice = localStorage.getItem('selectedMinerDevice');
        
        let devices: DetectedDevice[] = [];
        
        if (ilamResults) {
          devices = [...devices, ...JSON.parse(ilamResults)];
        }
        
        if (customResults) {
          devices = [...devices, ...JSON.parse(customResults)];
        }
        
        // Add selected device if exists
        if (selectedDevice) {
          const device = JSON.parse(selectedDevice);
          if (!devices.find(d => d.ip === device.ip)) {
            devices.push(device);
          }
        }
        
        setDetectedDevices(devices);
        console.log('دستگاه‌های ماینر بارگذاری شده:', devices.length);
      } catch (error) {
        console.error('خطا در بارگذاری دستگاه‌های شناسایی شده:', error);
      }
    };

    loadDetectedDevices();
    
    // Listen for storage changes
    const handleStorageChange = () => loadDetectedDevices();
    window.addEventListener('storage', handleStorageChange);
    
    return () => window.removeEventListener('storage', handleStorageChange);
  }, []);

  // Load Google Maps API
  useEffect(() => {
    const loadGoogleMaps = () => {
      if (window.google?.maps) {
        setIsLoaded(true);
        return;
      }

      const script = document.createElement('script');
      script.src = `https://maps.googleapis.com/maps/api/js?key=AIzaSyBDaeWicvigtP9xPv919E-RNoxfvC-Hqik&libraries=places`;
      script.async = true;
      script.defer = true;
      script.onload = () => setIsLoaded(true);
      script.onerror = () => console.error('خطا در بارگذاری Google Maps');
      
      document.head.appendChild(script);
    };

    loadGoogleMaps();
  }, []);

  // Initialize map when API is loaded
  useEffect(() => {
    if (!isLoaded || !mapRef.current || map) return;

    try {
      // Center on Ilam province
      const ilamCenter = { lat: 33.6374, lng: 46.4227 };
      
      const initialMap = new google.maps.Map(mapRef.current, {
        center: ilamCenter,
        zoom: 12,
        mapTypeId: google.maps.MapTypeId.ROADMAP
      });

      setMap(initialMap);
      console.log('نقشه Google Maps با موفقیت بارگذاری شد');
    } catch (error) {
      console.error('خطا در ایجاد نقشه:', error);
    }
  }, [isLoaded, map]);

  // Add markers for detected devices
  useEffect(() => {
    if (!map || !detectedDevices.length) return;

    // Clear existing markers
    markers.forEach(marker => marker.setMap(null));
    
    const newMarkers: google.maps.Marker[] = [];
    
    detectedDevices.forEach((device, index) => {
      const marker = new google.maps.Marker({
        position: { lat: device.location.lat, lng: device.location.lng },
        map: map,
        title: `${device.minerType} - ${device.ip}`,
        icon: {
          url: 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(`
            <svg width="32" height="32" viewBox="0 0 32 32" xmlns="http://www.w3.org/2000/svg">
              <circle cx="16" cy="16" r="12" fill="#ff0000" stroke="#000" stroke-width="2"/>
              <text x="16" y="20" text-anchor="middle" fill="#fff" font-size="12">M</text>
            </svg>
          `),
          scaledSize: new google.maps.Size(32, 32)
        }
      });

      // Add info window
      const infoWindow = new google.maps.InfoWindow({
        content: `
          <div style="font-family: 'Amiri Quran', serif; direction: rtl; padding: 10px;">
            <h3 style="color: #000; margin: 0 0 8px 0;">${device.minerType}</h3>
            <p style="margin: 4px 0; color: #333;"><strong>IP:</strong> ${device.ip}</p>
            <p style="margin: 4px 0; color: #333;"><strong>پورت‌ها:</strong> ${device.ports.join(', ')}</p>
            <p style="margin: 4px 0; color: #333;"><strong>موقعیت:</strong> ${device.location.city}, ${device.location.province}</p>
            ${device.location.isp ? `<p style="margin: 4px 0; color: #333;"><strong>ISP:</strong> ${device.location.isp}</p>` : ''}
            <p style="margin: 4px 0; color: #333;"><strong>اطمینان:</strong> ${device.confidence}%</p>
            <p style="margin: 4px 0; color: #333;"><strong>مختصات:</strong> ${device.location.lat.toFixed(4)}, ${device.location.lng.toFixed(4)}</p>
            <button onclick="routeToDevice(${device.location.lat}, ${device.location.lng})" 
                    style="background: #0000ff; color: white; padding: 5px 10px; border: none; margin-top: 8px; cursor: pointer;">
              مسیریابی
            </button>
          </div>
        `
      });

      marker.addListener('click', () => {
        infoWindow.open(map, marker);
      });

      newMarkers.push(marker);
    });
    
    setMarkers(newMarkers);
    
    // Adjust map bounds to show all markers
    if (newMarkers.length > 0) {
      const bounds = new google.maps.LatLngBounds();
      newMarkers.forEach(marker => {
        const position = marker.getPosition();
        if (position) bounds.extend(position);
      });
      map.fitBounds(bounds);
    }
    
    console.log('نشانگرهای دستگاه‌های ماینر اضافه شدند:', newMarkers.length);
  }, [map, detectedDevices]);

  // Get user location
  useEffect(() => {
    if (!map) return;

    if ('geolocation' in navigator) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          const location = {
            lat: position.coords.latitude,
            lng: position.coords.longitude
          };
          setUserLocation(location);
          
          // Add marker for user location
          new google.maps.Marker({
            position: location,
            map: map,
            title: 'موقعیت شما',
            icon: {
              url: 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(`
                <svg width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="12" cy="12" r="8" fill="#0000ff" stroke="#fff" stroke-width="2"/>
                </svg>
              `),
              scaledSize: new google.maps.Size(24, 24)
            }
          });

          console.log('موقعیت کاربر:', location);
        },
        (error) => console.log('خطا در دریافت موقعیت:', error),
        { enableHighAccuracy: true, timeout: 10000 }
      );
    }
  }, [map]);

  // Global function for routing from info window
  useEffect(() => {
    (window as any).routeToDevice = (lat: number, lng: number) => {
      if (userLocation) {
        handleRoutingTo({ lat, lng });
      } else {
        console.log('موقعیت کاربر شناسایی نشده است');
      }
    };
    
    return () => {
      delete (window as any).routeToDevice;
    };
  }, [userLocation]);

  const handleRouting = () => {
    if (!map || !userLocation) {
      console.log('نقشه یا موقعیت کاربر در دسترس نیست');
      return;
    }

    if (detectedDevices.length === 0) {
      console.log('هیچ دستگاه ماینری برای مسیریابی شناسایی نشده');
      return;
    }

    // Route to the nearest device
    let nearestDevice = detectedDevices[0];
    let minDistance = calculateDistance(userLocation, nearestDevice.location);

    detectedDevices.forEach(device => {
      const distance = calculateDistance(userLocation, device.location);
      if (distance < minDistance) {
        minDistance = distance;
        nearestDevice = device;
      }
    });

    handleRoutingTo(nearestDevice.location);
  };

  const handleRoutingTo = (destination: { lat: number; lng: number }) => {
    if (!map || !userLocation) return;

    console.log('مسیریابی شروع شد از موقعیت:', userLocation, 'به مقصد:', destination);
    
    // Clear previous route
    if (directionsRenderer) {
      directionsRenderer.setMap(null);
    }

    const directionsService = new google.maps.DirectionsService();
    const newDirectionsRenderer = new google.maps.DirectionsRenderer({
      polylineOptions: {
        strokeColor: '#ff0000',
        strokeOpacity: 0.8,
        strokeWeight: 4
      }
    });
    
    newDirectionsRenderer.setMap(map);
    setDirectionsRenderer(newDirectionsRenderer);
    
    directionsService.route({
      origin: userLocation,
      destination: destination,
      travelMode: google.maps.TravelMode.DRIVING
    }, (result: any, status: string) => {
      if (status === 'OK' && result) {
        newDirectionsRenderer.setDirections(result);
        console.log('مسیر محاسبه شد:', result);
        
        // Show route info
        const route = result.routes[0];
        const leg = route.legs[0];
        console.log(`مسافت: ${leg.distance.text}, زمان: ${leg.duration.text}`);
      } else {
        console.error('خطا در مسیریابی:', status);
      }
    });
  };

  const calculateDistance = (point1: { lat: number; lng: number }, point2: { lat: number; lng: number }) => {
    const R = 6371; // Earth's radius in kilometers
    const dLat = (point2.lat - point1.lat) * Math.PI / 180;
    const dLng = (point2.lng - point1.lng) * Math.PI / 180;
    const a = Math.sin(dLat/2) * Math.sin(dLat/2) +
              Math.cos(point1.lat * Math.PI / 180) * Math.cos(point2.lat * Math.PI / 180) *
              Math.sin(dLng/2) * Math.sin(dLng/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    return R * c;
  };

  const clearDetections = () => {
    localStorage.removeItem('ilamMinerResults');
    localStorage.removeItem('minerDetectionResults');
    localStorage.removeItem('selectedMinerDevice');
    setDetectedDevices([]);
    
    // Clear markers
    markers.forEach(marker => marker.setMap(null));
    setMarkers([]);
    
    // Clear route
    if (directionsRenderer) {
      directionsRenderer.setMap(null);
      setDirectionsRenderer(null);
    }
    
    console.log('همه تشخیص‌ها پاک شدند');
  };

  const clearRoute = () => {
    if (directionsRenderer) {
      directionsRenderer.setMap(null);
      setDirectionsRenderer(null);
      console.log('مسیر پاک شد');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-bold text-black" style={{ fontFamily: 'Amiri Quran' }}>
          نقشه هوشمند و موقعیت‌یابی ماینرها
        </h1>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Control Panel */}
        <div className="lg:col-span-1 space-y-4">
          <Card className="access-card">
            <CardHeader>
              <CardTitle className="text-base font-bold text-black flex items-center gap-2" style={{ fontFamily: 'Amiri Quran' }}>
                <Navigation className="w-5 h-5 text-black" />
                کنترل نقشه
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Button onClick={handleRouting} className="w-full access-button">
                <Route className="w-4 h-4 ml-2" />
                مسیریابی به نزدیکترین ماینر
              </Button>
              
              <Button onClick={clearRoute} className="w-full access-button">
                <Navigation className="w-4 h-4 ml-2" />
                پاک کردن مسیر
              </Button>
              
              <Button onClick={clearDetections} className="w-full access-button">
                <Radar className="w-4 h-4 ml-2" />
                پاک کردن تشخیص‌ها
              </Button>

              <div className="text-sm space-y-2" style={{ fontFamily: 'Amiri Quran', color: 'black' }}>
                <p><strong>وضعیت نقشه:</strong> {isLoaded ? 'بارگذاری شده' : 'در حال بارگذاری...'}</p>
                <p><strong>موقعیت:</strong> {userLocation ? 'شناسایی شده' : 'در حال جستجو...'}</p>
                <p><strong>دستگاه‌های شناسایی شده:</strong> {detectedDevices.length}</p>
              </div>

              {detectedDevices.length > 0 && (
                <div className="space-y-2">
                  <h4 className="text-sm font-bold text-black" style={{ fontFamily: 'Amiri Quran' }}>
                    <Zap className="w-4 h-4 inline ml-1" />
                    آمار ماینرها:
                  </h4>
                  <div className="text-xs space-y-1" style={{ fontFamily: 'Amiri Quran', color: 'black' }}>
                    <p>• Bitcoin: {detectedDevices.filter(d => d.minerType.includes('Bitcoin')).length}</p>
                    <p>• Ethereum: {detectedDevices.filter(d => d.minerType.includes('Ethereum')).length}</p>
                    <p>• سایر: {detectedDevices.filter(d => !d.minerType.includes('Bitcoin') && !d.minerType.includes('Ethereum')).length}</p>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Map Container */}
        <div className="lg:col-span-3">
          <Card className="access-card">
            <CardHeader>
              <CardTitle className="text-base font-bold text-black flex items-center gap-2" style={{ fontFamily: 'Amiri Quran' }}>
                <MapPin className="w-5 h-5 text-black" />
                نقشه تعاملی استان ایلام
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div 
                ref={mapRef}
                className="w-full h-96 bg-white border-2 border-gray-400"
                style={{ minHeight: '500px' }}
              >
                {!isLoaded && (
                  <div className="flex items-center justify-center h-full text-black" style={{ fontFamily: 'Amiri Quran' }}>
                    در حال بارگذاری نقشه Google Maps...
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
};

export default SmartMap;
