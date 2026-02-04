
import React, { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Progress } from "@/components/ui/progress";
import { Navigation, MapPin } from 'lucide-react';

interface HardwareDevice {
  id: string;
  name: string;
  type: string;
  status: 'connected' | 'disconnected' | 'error';
  driver: string;
  description: string;
}

const Hardware = () => {
  const [scanningDevices, setScanningDevices] = useState(false);
  const [scanProgress, setScanProgress] = useState(0);

  const hardwareDevices: HardwareDevice[] = [
    {
      id: 'rtl-sdr',
      name: 'RTL-SDR USB',
      type: 'Software Defined Radio',
      status: 'connected',
      driver: 'rtl-sdr-driver v1.2',
      description: 'دریافت سیگنال‌های رادیویی 24MHz تا 1.7GHz'
    },
    {
      id: 'thermal-cam',
      name: 'FLIR Lepton',
      type: 'Thermal Camera',
      status: 'disconnected',
      driver: 'درایور مورد نیاز',
      description: 'دوربین حرارتی برای تشخیص نقاط داغ'
    },
    {
      id: 'magnetometer',
      name: 'HMC5883L',
      type: 'Magnetometer',
      status: 'connected',
      driver: 'i2c-tools',
      description: 'سنسور میدان مغناطیسی سه‌محوره'
    },
    {
      id: 'gps-module',
      name: 'GPS NEO-8M',
      type: 'GPS Module',
      status: 'connected',
      driver: 'gpsd',
      description: 'موقعیت‌یاب جغرافیایی دقیق'
    }
  ];

  const startDeviceScan = () => {
    setScanningDevices(true);
    setScanProgress(0);
    
    const interval = setInterval(() => {
      setScanProgress(prev => {
        if (prev >= 100) {
          clearInterval(interval);
          setScanningDevices(false);
          return 100;
        }
        return prev + 5;
      });
    }, 200);
  };

  return (
    <div className="space-y-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground mb-2">
          تنظیمات سخت‌افزار
        </h1>
        <p className="text-muted-foreground">
          مدیریت و پیکربندی دستگاه‌های سخت‌افزاری متصل به سامانه
        </p>
      </div>

      <Tabs defaultValue="devices" className="space-y-6">
        <TabsList className="grid w-full grid-cols-3 bg-secondary">
          <TabsTrigger value="devices">دستگاه‌های متصل</TabsTrigger>
          <TabsTrigger value="drivers">مدیریت درایورها</TabsTrigger>
          <TabsTrigger value="calibration">کالیبراسیون</TabsTrigger>
        </TabsList>

        {/* Devices Tab */}
        <TabsContent value="devices" className="space-y-6">
          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Navigation className="w-5 h-5 text-primary" />
                اسکن دستگاه‌ها
              </CardTitle>
              <CardDescription>
                جستجو برای دستگاه‌های سخت‌افزاری متصل
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-4">
                <Button 
                  onClick={startDeviceScan}
                  disabled={scanningDevices}
                  className="bg-primary hover:bg-primary/90"
                >
                  {scanningDevices ? 'در حال اسکن...' : 'اسکن دستگاه‌ها'}
                </Button>

                {scanningDevices && (
                  <div className="flex-1">
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-sm text-muted-foreground">پیشرفت اسکن</span>
                      <span className="text-sm font-medium text-primary">{scanProgress}%</span>
                    </div>
                    <Progress value={scanProgress} className="h-2" />
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle>دستگاه‌های شناسایی شده</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {hardwareDevices.map((device) => (
                  <div key={device.id} className="p-4 bg-secondary/30 rounded-lg border border-border">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-4">
                        <div className={`w-3 h-3 rounded-full ${
                          device.status === 'connected' ? 'bg-accent detection-pulse' :
                          device.status === 'error' ? 'bg-destructive' : 'bg-muted-foreground'
                        }`}></div>
                        
                        <div>
                          <h4 className="font-medium text-foreground">{device.name}</h4>
                          <p className="text-sm text-muted-foreground">{device.description}</p>
                          <div className="flex items-center gap-4 mt-1">
                            <span className="text-xs text-primary font-medium">نوع: {device.type}</span>
                            <span className="text-xs text-muted-foreground">درایور: {device.driver}</span>
                          </div>
                        </div>
                      </div>
                      
                      <div className="flex items-center gap-3">
                        <Badge 
                          variant={device.status === 'connected' ? 'default' : 'outline'}
                          className={
                            device.status === 'connected' ? 'bg-accent text-accent-foreground' :
                            device.status === 'error' ? 'bg-destructive text-destructive-foreground' :
                            'border-muted-foreground text-muted-foreground'
                          }
                        >
                          {device.status === 'connected' ? 'متصل' :
                           device.status === 'error' ? 'خطا' : 'قطع'}
                        </Badge>
                        
                        <Button size="sm" variant="outline">
                          {device.status === 'connected' ? 'تنظیمات' : 'اتصال'}
                        </Button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Drivers Tab */}
        <TabsContent value="drivers" className="space-y-6">
          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <MapPin className="w-5 h-5 text-accent" />
                مدیریت درایورها
              </CardTitle>
              <CardDescription>
                نصب و به‌روزرسانی درایورهای سخت‌افزاری
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4">
                <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                  <div>
                    <div className="font-medium">RTL-SDR Drivers</div>
                    <div className="text-sm text-muted-foreground">درایور برای دستگاه‌های Software Defined Radio</div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className="border-accent text-accent">نصب شده</Badge>
                    <Button size="sm" variant="outline">به‌روزرسانی</Button>
                  </div>
                </div>

                <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                  <div>
                    <div className="font-medium">FLIR Thermal Camera</div>
                    <div className="text-sm text-muted-foreground">درایور دوربین‌های حرارتی FLIR</div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant="secondary">نیاز به نصب</Badge>
                    <Button size="sm" className="bg-primary hover:bg-primary/90">نصب</Button>
                  </div>
                </div>

                <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                  <div>
                    <div className="font-medium">USB-Serial Drivers</div>
                    <div className="text-sm text-muted-foreground">درایور برای ارتباط سریال با سنسورها</div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className="border-accent text-accent">نصب شده</Badge>
                    <Button size="sm" variant="outline">تنظیمات</Button>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="bg-amber-500/10 border-amber-500/20">
            <CardContent className="p-4">
              <p className="text-sm text-amber-700 dark:text-amber-300">
                <strong>هشدار:</strong> نصب درایورها ممکن است نیاز به دسترسی Administrator داشته باشد.
                پیش از نصب، اطمینان حاصل کنید که درایور از منبع معتبر دانلود شده است.
              </p>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Calibration Tab */}
        <TabsContent value="calibration" className="space-y-6">
          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle>کالیبراسیون دستگاه‌ها</CardTitle>
              <CardDescription>
                تنظیم دقیق و کالیبراسیون سنسورها
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* Magnetometer Calibration */}
              <div className="space-y-4">
                <h4 className="font-medium flex items-center gap-2">
                  <div className="w-3 h-3 bg-accent rounded-full"></div>
                  کالیبراسیون مگنتومتر
                </h4>
                <div className="p-4 bg-secondary/30 rounded-lg">
                  <div className="flex items-center justify-between mb-3">
                    <span className="text-sm">کالیبراسیون خودکار</span>
                    <Switch defaultChecked />
                  </div>
                  <div className="grid grid-cols-3 gap-4 text-center">
                    <div>
                      <div className="text-lg font-bold text-primary">X: 245</div>
                      <div className="text-xs text-muted-foreground">محور X</div>
                    </div>
                    <div>
                      <div className="text-lg font-bold text-primary">Y: 312</div>
                      <div className="text-xs text-muted-foreground">محور Y</div>
                    </div>
                    <div>
                      <div className="text-lg font-bold text-primary">Z: 189</div>
                      <div className="text-xs text-muted-foreground">محور Z</div>
                    </div>
                  </div>
                  <Button size="sm" variant="outline" className="w-full mt-3">
                    شروع کالیبراسیون
                  </Button>
                </div>
              </div>

              {/* RF Calibration */}
              <div className="space-y-4">
                <h4 className="font-medium flex items-center gap-2">
                  <div className="w-3 h-3 bg-primary rounded-full"></div>
                  کالیبراسیون SDR
                </h4>
                <div className="p-4 bg-secondary/30 rounded-lg">
                  <div className="grid grid-cols-2 gap-4 text-center mb-3">
                    <div>
                      <div className="text-lg font-bold text-accent">-2.1 ppm</div>
                      <div className="text-xs text-muted-foreground">انحراف فرکانس</div>
                    </div>
                    <div>
                      <div className="text-lg font-bold text-accent">48 dB</div>
                      <div className="text-xs text-muted-foreground">بهره آنتن</div>
                    </div>
                  </div>
                  <Button size="sm" variant="outline" className="w-full">
                    تنظیم مجدد فرکانس
                  </Button>
                </div>
              </div>

              {/* Thermal Calibration */}
              <div className="space-y-4">
                <h4 className="font-medium flex items-center gap-2">
                  <div className="w-3 h-3 bg-destructive rounded-full"></div>
                  کالیبراسیون حرارتی
                </h4>
                <div className="p-4 bg-secondary/30 rounded-lg">
                  <div className="grid grid-cols-2 gap-4 text-center mb-3">
                    <div>
                      <div className="text-lg font-bold text-destructive">25°C</div>
                      <div className="text-xs text-muted-foreground">دمای مرجع</div>
                    </div>
                    <div>
                      <div className="text-lg font-bold text-destructive">±1.5°C</div>
                      <div className="text-xs text-muted-foreground">خطای کالیبراسیون</div>
                    </div>
                  </div>
                  <Button size="sm" variant="outline" className="w-full">
                    کالیبراسیون دمایی
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default Hardware;
