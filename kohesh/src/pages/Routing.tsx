import React, { useState, useEffect, useRef } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Navigation2, MapPin, Route, Car, Clock, Ruler, Target, Loader2, Trash2, ExternalLink } from 'lucide-react';
import { useToast } from '@/hooks/use-toast';

interface DetectedDevice {
  ip: string;
  location: {
    lat: number;
    lng: number;
    city: string;
    province: string;
    isp?: string;
  };
  minerType: string;
  confidence: number;
}

interface RouteInfo {
  distance: string;
  duration: string;
  startAddress: string;
  endAddress: string;
}

const Routing = () => {
  const { toast } = useToast();
  const mapRef = useRef<HTMLDivElement>(null);
  const [map, setMap] = useState<google.maps.Map | null>(null);
  const [isLoaded, setIsLoaded] = useState(false);
  const [userLocation, setUserLocation] = useState<{ lat: number; lng: number } | null>(null);
  const [detectedDevices, setDetectedDevices] = useState<DetectedDevice[]>([]);
  const [selectedDevice, setSelectedDevice] = useState<DetectedDevice | null>(null);
  const [routeInfo, setRouteInfo] = useState<RouteInfo | null>(null);
  const [isCalculating, setIsCalculating] = useState(false);
  const [directionsRenderer, setDirectionsRenderer] = useState<google.maps.DirectionsRenderer | null>(null);
  const [markers, setMarkers] = useState<google.maps.Marker[]>([]);

  useEffect(() => {
    loadDetectedDevices();
    loadGoogleMaps();
    getUserLocation();
  }, []);

  const loadDetectedDevices = () => {
    try {
      const networkScanResults = localStorage.getItem('network_scan_results');
      const ilamResults = localStorage.getItem('ilamMinerResults');
      const remoteResults = localStorage.getItem('minerDetectionResults');
      
      let devices: DetectedDevice[] = [];
      
      if (networkScanResults) {
        const parsed = JSON.parse(networkScanResults);
        devices = [...devices, ...parsed];
      }
      if (ilamResults) {
        const parsed = JSON.parse(ilamResults);
        devices = [...devices, ...parsed];
      }
      if (remoteResults) {
        const parsed = JSON.parse(remoteResults);
        devices = [...devices, ...parsed];
      }
      
      const uniqueDevices = devices.filter((device, index, self) =>
        index === self.findIndex(d => d.ip === device.ip)
      );
      
      setDetectedDevices(uniqueDevices);
    } catch (error) {
      console.error('Error loading devices:', error);
    }
  };

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

  const getUserLocation = () => {
    if ('geolocation' in navigator) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          setUserLocation({
            lat: position.coords.latitude,
            lng: position.coords.longitude
          });
        },
        (error) => {
          console.log('خطا در دریافت موقعیت:', error);
          setUserLocation({ lat: 33.6374, lng: 46.4227 });
        },
        { enableHighAccuracy: true, timeout: 10000 }
      );
    } else {
      setUserLocation({ lat: 33.6374, lng: 46.4227 });
    }
  };

  useEffect(() => {
    if (!isLoaded || !mapRef.current || map) return;

    const center = userLocation || { lat: 33.6374, lng: 46.4227 };
    
    const initialMap = new google.maps.Map(mapRef.current, {
      center: center,
      zoom: 10,
      mapTypeId: google.maps.MapTypeId.ROADMAP
    });

    setMap(initialMap);
    
    const renderer = new google.maps.DirectionsRenderer({
      polylineOptions: {
        strokeColor: '#0000ff',
        strokeOpacity: 0.8,
        strokeWeight: 5
      }
    });
    renderer.setMap(initialMap);
    setDirectionsRenderer(renderer);
  }, [isLoaded, userLocation]);

  useEffect(() => {
    if (!map) return;

    markers.forEach(marker => marker.setMap(null));
    const newMarkers: google.maps.Marker[] = [];

    if (userLocation) {
      const userMarker = new google.maps.Marker({
        position: userLocation,
        map: map,
        title: 'موقعیت شما',
        icon: {
          url: 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(`
            <svg width="32" height="32" viewBox="0 0 32 32" xmlns="http://www.w3.org/2000/svg">
              <circle cx="16" cy="16" r="12" fill="#0000ff" stroke="#fff" stroke-width="3"/>
              <circle cx="16" cy="16" r="5" fill="#fff"/>
            </svg>
          `),
          scaledSize: new google.maps.Size(32, 32)
        }
      });
      newMarkers.push(userMarker);
    }

    detectedDevices.forEach((device, index) => {
      const marker = new google.maps.Marker({
        position: { lat: device.location.lat, lng: device.location.lng },
        map: map,
        title: `${device.minerType} - ${device.ip}`,
        icon: {
          url: 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(`
            <svg width="32" height="32" viewBox="0 0 32 32" xmlns="http://www.w3.org/2000/svg">
              <circle cx="16" cy="16" r="12" fill="#ff0000" stroke="#fff" stroke-width="2"/>
              <text x="16" y="20" text-anchor="middle" fill="#fff" font-size="12" font-weight="bold">M</text>
            </svg>
          `),
          scaledSize: new google.maps.Size(32, 32)
        }
      });

      marker.addListener('click', () => {
        setSelectedDevice(device);
      });

      newMarkers.push(marker);
    });

    setMarkers(newMarkers);
  }, [map, detectedDevices, userLocation]);

  const calculateRoute = async (destination: { lat: number; lng: number }) => {
    if (!map || !userLocation || !directionsRenderer) {
      toast({
        title: "خطا",
        description: "نقشه یا موقعیت شما در دسترس نیست",
        variant: "destructive"
      });
      return;
    }

    setIsCalculating(true);

    const directionsService = new google.maps.DirectionsService();
    
    directionsService.route({
      origin: userLocation,
      destination: destination,
      travelMode: google.maps.TravelMode.DRIVING
    }, (result, status) => {
      setIsCalculating(false);
      
      if (status === 'OK' && result) {
        directionsRenderer.setDirections(result);
        
        const route = result.routes[0];
        const leg = route.legs[0];
        
        setRouteInfo({
          distance: leg.distance?.text || 'نامشخص',
          duration: leg.duration?.text || 'نامشخص',
          startAddress: leg.start_address || '',
          endAddress: leg.end_address || ''
        });
        
        toast({
          title: "مسیر محاسبه شد",
          description: `مسافت: ${leg.distance?.text} - زمان: ${leg.duration?.text}`
        });
      } else {
        toast({
          title: "خطا در مسیریابی",
          description: "امکان محاسبه مسیر وجود ندارد",
          variant: "destructive"
        });
      }
    });
  };

  const routeToNearest = () => {
    if (!userLocation || detectedDevices.length === 0) {
      toast({
        title: "خطا",
        description: "موقعیت شما یا دستگاه‌های شناسایی شده در دسترس نیست",
        variant: "destructive"
      });
      return;
    }

    let nearest = detectedDevices[0];
    let minDistance = calculateDistance(userLocation, nearest.location);

    detectedDevices.forEach(device => {
      const distance = calculateDistance(userLocation, device.location);
      if (distance < minDistance) {
        minDistance = distance;
        nearest = device;
      }
    });

    setSelectedDevice(nearest);
    calculateRoute(nearest.location);
  };

  const calculateDistance = (point1: { lat: number; lng: number }, point2: { lat: number; lng: number }) => {
    const R = 6371;
    const dLat = (point2.lat - point1.lat) * Math.PI / 180;
    const dLng = (point2.lng - point1.lng) * Math.PI / 180;
    const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
              Math.cos(point1.lat * Math.PI / 180) * Math.cos(point2.lat * Math.PI / 180) *
              Math.sin(dLng / 2) * Math.sin(dLng / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
  };

  const clearRoute = () => {
    if (directionsRenderer) {
      directionsRenderer.setDirections({ routes: [] } as any);
    }
    setRouteInfo(null);
    setSelectedDevice(null);
  };

  const openInGoogleMaps = () => {
    if (!userLocation || !selectedDevice) return;
    
    const url = `https://www.google.com/maps/dir/?api=1&origin=${userLocation.lat},${userLocation.lng}&destination=${selectedDevice.location.lat},${selectedDevice.location.lng}&travelmode=driving`;
    window.open(url, '_blank');
  };

  return (
    <div className="space-y-6" dir="rtl">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 mb-1" style={{ fontFamily: 'BNazanin' }}>
            سیستم مسیریابی
          </h1>
          <p className="text-sm text-gray-600" style={{ fontFamily: 'BNazanin' }}>
            مسیریابی هوشمند به مکان ماینرهای شناسایی شده
          </p>
        </div>
        <Badge variant="outline" className="px-4 py-2">
          <Navigation2 className="w-4 h-4 ml-2" />
          {detectedDevices.length} دستگاه شناسایی شده
        </Badge>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        <div className="lg:col-span-1 space-y-4">
          <Card className="access-card">
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                <Route className="w-5 h-5" />
                کنترل مسیریابی
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Button 
                onClick={routeToNearest} 
                disabled={isCalculating || detectedDevices.length === 0}
                className="w-full access-button"
              >
                {isCalculating ? (
                  <Loader2 className="w-4 h-4 ml-2 animate-spin" />
                ) : (
                  <Target className="w-4 h-4 ml-2" />
                )}
                مسیریابی به نزدیکترین
              </Button>
              
              <Button 
                onClick={clearRoute} 
                variant="outline" 
                className="w-full"
              >
                <Trash2 className="w-4 h-4 ml-2" />
                پاک کردن مسیر
              </Button>

              {selectedDevice && (
                <Button 
                  onClick={openInGoogleMaps} 
                  variant="outline" 
                  className="w-full text-blue-600 border-blue-300 hover:bg-blue-50"
                >
                  <ExternalLink className="w-4 h-4 ml-2" />
                  باز کردن در Google Maps
                </Button>
              )}

              <div className="pt-4 border-t space-y-2 text-sm" style={{ fontFamily: 'BNazanin' }}>
                <div className="flex justify-between">
                  <span className="text-gray-500">وضعیت نقشه:</span>
                  <span className={isLoaded ? 'text-green-600' : 'text-yellow-600'}>
                    {isLoaded ? 'آماده' : 'در حال بارگذاری...'}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">موقعیت شما:</span>
                  <span className={userLocation ? 'text-green-600' : 'text-yellow-600'}>
                    {userLocation ? 'شناسایی شده' : 'در حال جستجو...'}
                  </span>
                </div>
              </div>
            </CardContent>
          </Card>

          {routeInfo && (
            <Card className="access-card border-blue-200 bg-blue-50">
              <CardHeader className="pb-2">
                <CardTitle className="text-base flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                  <Car className="w-5 h-5 text-blue-600" />
                  اطلاعات مسیر
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex items-center gap-3 p-3 bg-white rounded-lg">
                  <Ruler className="w-5 h-5 text-blue-600" />
                  <div>
                    <div className="text-xs text-gray-500" style={{ fontFamily: 'BNazanin' }}>مسافت</div>
                    <div className="font-bold">{routeInfo.distance}</div>
                  </div>
                </div>
                <div className="flex items-center gap-3 p-3 bg-white rounded-lg">
                  <Clock className="w-5 h-5 text-blue-600" />
                  <div>
                    <div className="text-xs text-gray-500" style={{ fontFamily: 'BNazanin' }}>زمان تقریبی</div>
                    <div className="font-bold">{routeInfo.duration}</div>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          {selectedDevice && (
            <Card className="access-card border-red-200 bg-red-50">
              <CardHeader className="pb-2">
                <CardTitle className="text-base flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                  <MapPin className="w-5 h-5 text-red-600" />
                  دستگاه انتخاب شده
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm" style={{ fontFamily: 'BNazanin' }}>
                <div className="flex justify-between">
                  <span className="text-gray-500">IP:</span>
                  <span className="font-mono">{selectedDevice.ip}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">نوع:</span>
                  <span>{selectedDevice.minerType}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">موقعیت:</span>
                  <span>{selectedDevice.location.city}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">اطمینان:</span>
                  <span>{selectedDevice.confidence}%</span>
                </div>
                <Button 
                  onClick={() => calculateRoute(selectedDevice.location)}
                  disabled={isCalculating}
                  className="w-full mt-2 access-button"
                  size="sm"
                >
                  محاسبه مسیر
                </Button>
              </CardContent>
            </Card>
          )}
        </div>

        <div className="lg:col-span-3">
          <Card className="access-card">
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                <MapPin className="w-5 h-5" />
                نقشه مسیریابی
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div 
                ref={mapRef}
                className="w-full h-96 bg-gray-100 border-2 border-gray-300 rounded-lg"
                style={{ minHeight: '500px' }}
              >
                {!isLoaded && (
                  <div className="flex items-center justify-center h-full" style={{ fontFamily: 'BNazanin' }}>
                    <Loader2 className="w-8 h-8 animate-spin text-blue-600 ml-2" />
                    در حال بارگذاری نقشه...
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {detectedDevices.length > 0 && (
            <Card className="access-card mt-4">
              <CardHeader>
                <CardTitle className="text-base flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                  <Target className="w-5 h-5" />
                  لیست دستگاه‌های شناسایی شده
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3 max-h-64 overflow-y-auto">
                  {detectedDevices.map((device, index) => (
                    <div 
                      key={index}
                      onClick={() => {
                        setSelectedDevice(device);
                        if (map) {
                          map.panTo(device.location);
                          map.setZoom(14);
                        }
                      }}
                      className={`p-3 border rounded-lg cursor-pointer transition-all hover:shadow-md ${
                        selectedDevice?.ip === device.ip 
                          ? 'border-blue-500 bg-blue-50' 
                          : 'border-gray-200 hover:border-blue-300'
                      }`}
                    >
                      <div className="text-xs font-mono text-gray-600">{device.ip}</div>
                      <div className="text-sm font-medium" style={{ fontFamily: 'BNazanin' }}>{device.minerType}</div>
                      <div className="text-xs text-gray-500" style={{ fontFamily: 'BNazanin' }}>{device.location.city}</div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
};

export default Routing;
