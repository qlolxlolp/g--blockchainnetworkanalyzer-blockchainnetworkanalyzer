import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Slider } from "@/components/ui/slider";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { Settings, Gauge, Wrench, Wifi, Radio, Thermometer, CheckCircle, AlertTriangle, RotateCcw, Save } from 'lucide-react';
import { useToast } from '@/hooks/use-toast';

interface CalibrationSettings {
  rfSensitivity: number;
  magneticSensitivity: number;
  thermalThreshold: number;
  networkTimeout: number;
  scanInterval: number;
  autoCalibrate: boolean;
  noiseFilter: boolean;
  signalBoost: boolean;
}

interface CalibrationStatus {
  rf: 'uncalibrated' | 'calibrating' | 'calibrated' | 'error';
  magnetic: 'uncalibrated' | 'calibrating' | 'calibrated' | 'error';
  thermal: 'uncalibrated' | 'calibrating' | 'calibrated' | 'error';
  network: 'uncalibrated' | 'calibrating' | 'calibrated' | 'error';
}

const Calibration = () => {
  const { toast } = useToast();
  
  const [settings, setSettings] = useState<CalibrationSettings>({
    rfSensitivity: 75,
    magneticSensitivity: 80,
    thermalThreshold: 65,
    networkTimeout: 5000,
    scanInterval: 1000,
    autoCalibrate: true,
    noiseFilter: true,
    signalBoost: false
  });
  
  const [status, setStatus] = useState<CalibrationStatus>({
    rf: 'uncalibrated',
    magnetic: 'uncalibrated',
    thermal: 'uncalibrated',
    network: 'uncalibrated'
  });
  
  const [calibrationProgress, setCalibrationProgress] = useState(0);
  const [isCalibrating, setIsCalibrating] = useState(false);

  useEffect(() => {
    const savedSettings = localStorage.getItem('calibration_settings');
    if (savedSettings) {
      setSettings(JSON.parse(savedSettings));
    }
    
    const savedStatus = localStorage.getItem('calibration_status');
    if (savedStatus) {
      setStatus(JSON.parse(savedStatus));
    }
  }, []);

  const saveSettings = () => {
    localStorage.setItem('calibration_settings', JSON.stringify(settings));
    toast({
      title: "تنظیمات ذخیره شد",
      description: "تنظیمات کالیبراسیون با موفقیت ذخیره شدند."
    });
  };

  const calibrateSensor = async (sensorType: keyof CalibrationStatus) => {
    setStatus(prev => ({ ...prev, [sensorType]: 'calibrating' }));
    setIsCalibrating(true);
    setCalibrationProgress(0);
    
    const steps = [
      'شروع کالیبراسیون...',
      'قرائت مقادیر پایه...',
      'تنظیم حساسیت...',
      'فیلتر نویز...',
      'تایید نهایی...'
    ];
    
    for (let i = 0; i <= 100; i += 2) {
      await new Promise(r => setTimeout(r, 50));
      setCalibrationProgress(i);
    }
    
    const success = Math.random() > 0.1;
    
    setStatus(prev => ({ 
      ...prev, 
      [sensorType]: success ? 'calibrated' : 'error' 
    }));
    
    localStorage.setItem('calibration_status', JSON.stringify({
      ...status,
      [sensorType]: success ? 'calibrated' : 'error'
    }));
    
    setIsCalibrating(false);
    
    toast({
      title: success ? "کالیبراسیون موفق" : "خطا در کالیبراسیون",
      description: success 
        ? `سنسور ${getSensorName(sensorType)} با موفقیت کالیبره شد.`
        : `لطفاً دوباره تلاش کنید.`,
      variant: success ? "default" : "destructive"
    });
  };

  const calibrateAll = async () => {
    const sensors: (keyof CalibrationStatus)[] = ['rf', 'magnetic', 'thermal', 'network'];
    for (const sensor of sensors) {
      await calibrateSensor(sensor);
      await new Promise(r => setTimeout(r, 500));
    }
  };

  const resetCalibration = () => {
    setStatus({
      rf: 'uncalibrated',
      magnetic: 'uncalibrated',
      thermal: 'uncalibrated',
      network: 'uncalibrated'
    });
    localStorage.removeItem('calibration_status');
    toast({
      title: "کالیبراسیون ریست شد",
      description: "همه تنظیمات کالیبراسیون پاک شدند."
    });
  };

  const getSensorName = (type: string) => {
    const names: Record<string, string> = {
      rf: 'رادیویی',
      magnetic: 'مغناطیسی',
      thermal: 'حرارتی',
      network: 'شبکه'
    };
    return names[type] || type;
  };

  const getStatusBadge = (statusType: string) => {
    switch (statusType) {
      case 'calibrated':
        return <Badge className="bg-green-100 text-green-800"><CheckCircle className="w-3 h-3 ml-1" />کالیبره شده</Badge>;
      case 'calibrating':
        return <Badge className="bg-blue-100 text-blue-800 animate-pulse">در حال کالیبراسیون...</Badge>;
      case 'error':
        return <Badge className="bg-red-100 text-red-800"><AlertTriangle className="w-3 h-3 ml-1" />خطا</Badge>;
      default:
        return <Badge className="bg-gray-100 text-gray-800">کالیبره نشده</Badge>;
    }
  };

  return (
    <div className="space-y-6" dir="rtl">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 mb-1" style={{ fontFamily: 'BNazanin' }}>
            کالیبراسیون سنسورها
          </h1>
          <p className="text-sm text-gray-600" style={{ fontFamily: 'BNazanin' }}>
            تنظیم دقیق حساسگرها برای بهینه‌سازی دقت تشخیص
          </p>
        </div>
        <div className="flex gap-2">
          <Button onClick={calibrateAll} disabled={isCalibrating} className="access-button">
            <Gauge className="w-4 h-4 ml-2" />
            کالیبراسیون همه
          </Button>
          <Button onClick={resetCalibration} variant="outline" className="border-red-300 text-red-600 hover:bg-red-50">
            <RotateCcw className="w-4 h-4 ml-2" />
            ریست
          </Button>
        </div>
      </div>

      {isCalibrating && (
        <Card className="access-card border-blue-200 bg-blue-50">
          <CardContent className="pt-4">
            <div className="space-y-2">
              <div className="flex justify-between text-sm" style={{ fontFamily: 'BNazanin' }}>
                <span>پیشرفت کالیبراسیون</span>
                <span>{calibrationProgress}%</span>
              </div>
              <Progress value={calibrationProgress} className="h-2" />
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="access-card">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <Radio className="w-5 h-5 text-blue-600" />
              سنسور رادیویی (RF)
              {getStatusBadge(status.rf)}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label className="text-sm" style={{ fontFamily: 'BNazanin' }}>حساسیت RF: {settings.rfSensitivity}%</Label>
              <Slider
                value={[settings.rfSensitivity]}
                onValueChange={(val) => setSettings(prev => ({ ...prev, rfSensitivity: val[0] }))}
                max={100}
                min={10}
                step={5}
              />
            </div>
            <Button 
              onClick={() => calibrateSensor('rf')} 
              disabled={isCalibrating}
              className="w-full access-button"
            >
              کالیبره کردن سنسور RF
            </Button>
          </CardContent>
        </Card>

        <Card className="access-card">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <Settings className="w-5 h-5 text-purple-600" />
              سنسور مغناطیسی
              {getStatusBadge(status.magnetic)}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label className="text-sm" style={{ fontFamily: 'BNazanin' }}>حساسیت مغناطیسی: {settings.magneticSensitivity}%</Label>
              <Slider
                value={[settings.magneticSensitivity]}
                onValueChange={(val) => setSettings(prev => ({ ...prev, magneticSensitivity: val[0] }))}
                max={100}
                min={10}
                step={5}
              />
            </div>
            <Button 
              onClick={() => calibrateSensor('magnetic')} 
              disabled={isCalibrating}
              className="w-full access-button"
            >
              کالیبره کردن سنسور مغناطیسی
            </Button>
          </CardContent>
        </Card>

        <Card className="access-card">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <Thermometer className="w-5 h-5 text-red-600" />
              سنسور حرارتی
              {getStatusBadge(status.thermal)}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label className="text-sm" style={{ fontFamily: 'BNazanin' }}>آستانه دمایی: {settings.thermalThreshold}°C</Label>
              <Slider
                value={[settings.thermalThreshold]}
                onValueChange={(val) => setSettings(prev => ({ ...prev, thermalThreshold: val[0] }))}
                max={100}
                min={30}
                step={5}
              />
            </div>
            <Button 
              onClick={() => calibrateSensor('thermal')} 
              disabled={isCalibrating}
              className="w-full access-button"
            >
              کالیبره کردن سنسور حرارتی
            </Button>
          </CardContent>
        </Card>

        <Card className="access-card">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <Wifi className="w-5 h-5 text-green-600" />
              اسکنر شبکه
              {getStatusBadge(status.network)}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label className="text-sm" style={{ fontFamily: 'BNazanin' }}>زمان انتظار: {settings.networkTimeout}ms</Label>
              <Slider
                value={[settings.networkTimeout]}
                onValueChange={(val) => setSettings(prev => ({ ...prev, networkTimeout: val[0] }))}
                max={30000}
                min={1000}
                step={500}
              />
            </div>
            <Button 
              onClick={() => calibrateSensor('network')} 
              disabled={isCalibrating}
              className="w-full access-button"
            >
              کالیبره کردن اسکنر شبکه
            </Button>
          </CardContent>
        </Card>
      </div>

      <Card className="access-card">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
            <Wrench className="w-5 h-5" />
            تنظیمات پیشرفته
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
              <Label className="text-sm" style={{ fontFamily: 'BNazanin' }}>کالیبراسیون خودکار</Label>
              <Switch
                checked={settings.autoCalibrate}
                onCheckedChange={(checked) => setSettings(prev => ({ ...prev, autoCalibrate: checked }))}
              />
            </div>
            <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
              <Label className="text-sm" style={{ fontFamily: 'BNazanin' }}>فیلتر نویز</Label>
              <Switch
                checked={settings.noiseFilter}
                onCheckedChange={(checked) => setSettings(prev => ({ ...prev, noiseFilter: checked }))}
              />
            </div>
            <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
              <Label className="text-sm" style={{ fontFamily: 'BNazanin' }}>تقویت سیگنال</Label>
              <Switch
                checked={settings.signalBoost}
                onCheckedChange={(checked) => setSettings(prev => ({ ...prev, signalBoost: checked }))}
              />
            </div>
          </div>
          
          <div className="mt-6 flex justify-end">
            <Button onClick={saveSettings} className="access-button">
              <Save className="w-4 h-4 ml-2" />
              ذخیره تنظیمات
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default Calibration;
