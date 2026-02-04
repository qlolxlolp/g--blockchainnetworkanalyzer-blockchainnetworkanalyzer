
import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { MapPin, Zap, Thermometer, Wifi } from 'lucide-react';

interface MinerLocation {
  id: string;
  name: string;
  lat: number;
  lng: number;
  hashRate: number;
  temperature: number;
  status: 'active' | 'warning' | 'offline';
  signal: number;
}

export function MinerMap() {
  const [miners, setMiners] = useState<MinerLocation[]>([
    { id: '001', name: 'ماینر تهران', lat: 35.6892, lng: 51.3890, hashRate: 14.2, temperature: 65, status: 'active', signal: 85 },
    { id: '002', name: 'ماینر اصفهان', lat: 32.6546, lng: 51.6680, hashRate: 12.8, temperature: 72, status: 'warning', signal: 78 },
    { id: '003', name: 'ماینر شیراز', lat: 29.5918, lng: 52.5837, hashRate: 15.1, temperature: 58, status: 'active', signal: 92 },
    { id: '004', name: 'ماینر مشهد', lat: 36.2605, lng: 59.6168, hashRate: 0, temperature: 45, status: 'offline', signal: 0 },
    { id: '005', name: 'ماینر تبریز', lat: 38.0962, lng: 46.2738, hashRate: 13.5, temperature: 68, status: 'active', signal: 88 }
  ]);

  useEffect(() => {
    const interval = setInterval(() => {
      setMiners(prev => prev.map(miner => ({
        ...miner,
        hashRate: miner.status === 'offline' ? 0 : +(miner.hashRate + (Math.random() - 0.5) * 2).toFixed(1),
        temperature: Math.max(40, Math.min(85, miner.temperature + Math.floor(Math.random() * 6) - 3)),
        signal: miner.status === 'offline' ? 0 : Math.max(50, Math.min(100, miner.signal + Math.floor(Math.random() * 10) - 5))
      })));
    }, 5000);

    return () => clearInterval(interval);
  }, []);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active': return 'text-green-500';
      case 'warning': return 'text-yellow-500';
      case 'offline': return 'text-red-500';
      default: return 'text-gray-500';
    }
  };

  const getStatusBg = (status: string) => {
    switch (status) {
      case 'active': return 'bg-green-500/20';
      case 'warning': return 'bg-yellow-500/20';
      case 'offline': return 'bg-red-500/20';
      default: return 'bg-gray-500/20';
    }
  };

  return (
    <Card className="bg-card/50 backdrop-blur-sm border-border">
      <CardHeader>
        <CardTitle className="text-lg font-normal flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
          <MapPin className="w-5 h-5 text-primary" />
          نقشه ماینرهای شناسایی شده
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {miners.map((miner) => (
            <div 
              key={miner.id} 
              className={`p-4 rounded-lg border transition-all duration-300 hover:scale-105 ${getStatusBg(miner.status)}`}
            >
              <div className="flex items-center justify-between mb-3">
                <h3 className="font-normal text-sm" style={{ fontFamily: 'BNazanin' }}>{miner.name}</h3>
                <span className={`text-xs px-2 py-1 rounded-full ${getStatusColor(miner.status)} bg-current/20`}>
                  {miner.status === 'active' ? 'فعال' : miner.status === 'warning' ? 'هشدار' : 'آفلاین'}
                </span>
              </div>
              
              <div className="space-y-2 text-xs">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1">
                    <Zap className="w-3 h-3 text-blue-500" />
                    <span>نرخ هش:</span>
                  </div>
                  <span className="font-mono">{miner.hashRate} TH/s</span>
                </div>
                
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1">
                    <Thermometer className="w-3 h-3 text-red-500" />
                    <span>دما:</span>
                  </div>
                  <span className="font-mono">{miner.temperature}°C</span>
                </div>
                
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1">
                    <Wifi className="w-3 h-3 text-green-500" />
                    <span>سیگنال:</span>
                  </div>
                  <span className="font-mono">{miner.signal}%</span>
                </div>
                
                <div className="text-xs text-muted-foreground mt-2">
                  <span>موقعیت: {miner.lat.toFixed(4)}, {miner.lng.toFixed(4)}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
