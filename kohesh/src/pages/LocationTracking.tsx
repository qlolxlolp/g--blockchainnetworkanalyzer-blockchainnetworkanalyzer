
import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { MapPin, Crosshair, Target } from 'lucide-react';

const LocationTracking = () => {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-normal text-foreground" style={{ fontFamily: 'BNazanin' }}>
          سیستم مکان‌یابی
        </h1>
        <div className="flex items-center gap-2 px-4 py-2 bg-primary/10 rounded-lg">
          <Target className="w-4 h-4 text-primary" />
          <span className="text-sm text-primary">ردیابی فعال</span>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="bg-card/50 backdrop-blur-sm border-border">
          <CardHeader>
            <CardTitle className="text-lg font-normal flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <Crosshair className="w-5 h-5 text-primary" />
              ردیابی دقیق موقعیت
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground" style={{ fontFamily: 'BNazanin' }}>
              سیستم ردیابی موقعیت دقیق ماینرها با استفاده از GPS و تریانگولاسیون سیگنال
            </p>
          </CardContent>
        </Card>

        <Card className="bg-card/50 backdrop-blur-sm border-border">
          <CardHeader>
            <CardTitle className="text-lg font-normal flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <MapPin className="w-5 h-5 text-primary" />
              تاریخچه مکان‌ها
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground" style={{ fontFamily: 'BNazanin' }}>
              نگهداری تاریخچه حرکات و تغییرات مکان دستگاه‌های ماینر
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default LocationTracking;
