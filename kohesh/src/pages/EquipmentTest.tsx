import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { TestTube, CheckCircle, AlertTriangle, XCircle, Play, RotateCcw, Cpu, Wifi, HardDrive, Thermometer, Radio, Activity } from 'lucide-react';
import { useToast } from '@/hooks/use-toast';

interface TestResult {
  name: string;
  status: 'pending' | 'running' | 'passed' | 'failed' | 'warning';
  message: string;
  duration?: number;
  details?: string;
}

interface SystemInfo {
  browser: string;
  platform: string;
  memory: string;
  cores: number;
  online: boolean;
  geolocation: boolean;
  webgl: boolean;
  localStorage: boolean;
}

const EquipmentTest = () => {
  const { toast } = useToast();
  
  const [tests, setTests] = useState<TestResult[]>([
    { name: 'اتصال اینترنت', status: 'pending', message: 'در انتظار تست...' },
    { name: 'حافظه مرورگر', status: 'pending', message: 'در انتظار تست...' },
    { name: 'موقعیت‌یابی GPS', status: 'pending', message: 'در انتظار تست...' },
    { name: 'WebGL و گرافیک', status: 'pending', message: 'در انتظار تست...' },
    { name: 'ذخیره‌سازی محلی', status: 'pending', message: 'در انتظار تست...' },
    { name: 'Web Serial API', status: 'pending', message: 'در انتظار تست...' },
    { name: 'اتصال به Supabase', status: 'pending', message: 'در انتظار تست...' },
    { name: 'سرعت پردازش', status: 'pending', message: 'در انتظار تست...' }
  ]);
  
  const [isRunning, setIsRunning] = useState(false);
  const [overallProgress, setOverallProgress] = useState(0);
  const [systemInfo, setSystemInfo] = useState<SystemInfo | null>(null);

  useEffect(() => {
    detectSystemInfo();
  }, []);

  const detectSystemInfo = () => {
    const info: SystemInfo = {
      browser: navigator.userAgent.includes('Chrome') ? 'Chrome' : 
               navigator.userAgent.includes('Firefox') ? 'Firefox' : 
               navigator.userAgent.includes('Safari') ? 'Safari' : 'Unknown',
      platform: navigator.platform,
      memory: (navigator as any).deviceMemory ? `${(navigator as any).deviceMemory} GB` : 'نامشخص',
      cores: navigator.hardwareConcurrency || 1,
      online: navigator.onLine,
      geolocation: 'geolocation' in navigator,
      webgl: (() => {
        try {
          const canvas = document.createElement('canvas');
          return !!(canvas.getContext('webgl') || canvas.getContext('experimental-webgl'));
        } catch (e) {
          return false;
        }
      })(),
      localStorage: (() => {
        try {
          localStorage.setItem('test', 'test');
          localStorage.removeItem('test');
          return true;
        } catch (e) {
          return false;
        }
      })()
    };
    setSystemInfo(info);
  };

  const runAllTests = async () => {
    setIsRunning(true);
    setOverallProgress(0);
    
    const testFunctions = [
      testInternetConnection,
      testBrowserMemory,
      testGeolocation,
      testWebGL,
      testLocalStorage,
      testWebSerial,
      testSupabaseConnection,
      testProcessingSpeed
    ];
    
    for (let i = 0; i < testFunctions.length; i++) {
      setTests(prev => prev.map((test, idx) => 
        idx === i ? { ...test, status: 'running', message: 'در حال تست...' } : test
      ));
      
      await testFunctions[i](i);
      setOverallProgress(((i + 1) / testFunctions.length) * 100);
      
      await new Promise(r => setTimeout(r, 300));
    }
    
    setIsRunning(false);
    
    const passedCount = tests.filter(t => t.status === 'passed').length;
    toast({
      title: "تست‌ها تکمیل شد",
      description: `${passedCount} از ${tests.length} تست موفق بود.`,
      variant: passedCount === tests.length ? "default" : "destructive"
    });
  };

  const testInternetConnection = async (index: number) => {
    const startTime = Date.now();
    try {
      const response = await fetch('https://www.google.com/generate_204', { mode: 'no-cors' });
      const duration = Date.now() - startTime;
      setTests(prev => prev.map((test, idx) => 
        idx === index ? { 
          ...test, 
          status: 'passed', 
          message: `اتصال برقرار (${duration}ms)`,
          duration 
        } : test
      ));
    } catch (error) {
      setTests(prev => prev.map((test, idx) => 
        idx === index ? { 
          ...test, 
          status: 'failed', 
          message: 'اتصال اینترنت برقرار نیست'
        } : test
      ));
    }
  };

  const testBrowserMemory = async (index: number) => {
    const memory = (performance as any).memory;
    if (memory) {
      const usedMB = Math.round(memory.usedJSHeapSize / 1024 / 1024);
      const totalMB = Math.round(memory.jsHeapSizeLimit / 1024 / 1024);
      const percentage = Math.round((usedMB / totalMB) * 100);
      
      setTests(prev => prev.map((test, idx) => 
        idx === index ? { 
          ...test, 
          status: percentage < 80 ? 'passed' : 'warning', 
          message: `${usedMB}MB از ${totalMB}MB استفاده شده (${percentage}%)`,
          details: percentage >= 80 ? 'حافظه تقریباً پر است' : undefined
        } : test
      ));
    } else {
      setTests(prev => prev.map((test, idx) => 
        idx === index ? { 
          ...test, 
          status: 'warning', 
          message: 'API حافظه در دسترس نیست'
        } : test
      ));
    }
  };

  const testGeolocation = async (index: number) => {
    if ('geolocation' in navigator) {
      try {
        const position = await new Promise<GeolocationPosition>((resolve, reject) => {
          navigator.geolocation.getCurrentPosition(resolve, reject, { timeout: 10000 });
        });
        
        setTests(prev => prev.map((test, idx) => 
          idx === index ? { 
            ...test, 
            status: 'passed', 
            message: `موقعیت: ${position.coords.latitude.toFixed(4)}, ${position.coords.longitude.toFixed(4)}`,
            details: `دقت: ${position.coords.accuracy}m`
          } : test
        ));
      } catch (error) {
        setTests(prev => prev.map((test, idx) => 
          idx === index ? { 
            ...test, 
            status: 'warning', 
            message: 'دسترسی به موقعیت رد شد یا خطا رخ داد'
          } : test
        ));
      }
    } else {
      setTests(prev => prev.map((test, idx) => 
        idx === index ? { 
          ...test, 
          status: 'failed', 
          message: 'Geolocation پشتیبانی نمی‌شود'
        } : test
      ));
    }
  };

  const testWebGL = async (index: number) => {
    try {
      const canvas = document.createElement('canvas');
      const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
      
      if (gl) {
        const debugInfo = gl.getExtension('WEBGL_debug_renderer_info');
        const renderer = debugInfo ? gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL) : 'Unknown';
        
        setTests(prev => prev.map((test, idx) => 
          idx === index ? { 
            ...test, 
            status: 'passed', 
            message: 'WebGL فعال است',
            details: renderer
          } : test
        ));
      } else {
        throw new Error('WebGL not supported');
      }
    } catch (error) {
      setTests(prev => prev.map((test, idx) => 
        idx === index ? { 
          ...test, 
          status: 'failed', 
          message: 'WebGL پشتیبانی نمی‌شود'
        } : test
      ));
    }
  };

  const testLocalStorage = async (index: number) => {
    try {
      const testKey = 'equipment_test_' + Date.now();
      const testData = JSON.stringify({ test: true, timestamp: Date.now() });
      
      localStorage.setItem(testKey, testData);
      const retrieved = localStorage.getItem(testKey);
      localStorage.removeItem(testKey);
      
      if (retrieved === testData) {
        let totalSize = 0;
        for (const key in localStorage) {
          if (Object.prototype.hasOwnProperty.call(localStorage, key)) {
            totalSize += localStorage[key].length * 2;
          }
        }
        const usedKB = Math.round(totalSize / 1024);
        
        setTests(prev => prev.map((test, idx) => 
          idx === index ? { 
            ...test, 
            status: 'passed', 
            message: `ذخیره‌سازی فعال (${usedKB}KB استفاده شده)`
          } : test
        ));
      } else {
        throw new Error('Data mismatch');
      }
    } catch (error) {
      setTests(prev => prev.map((test, idx) => 
        idx === index ? { 
          ...test, 
          status: 'failed', 
          message: 'خطا در ذخیره‌سازی محلی'
        } : test
      ));
    }
  };

  const testWebSerial = async (index: number) => {
    if ('serial' in navigator) {
      setTests(prev => prev.map((test, idx) => 
        idx === index ? { 
          ...test, 
          status: 'passed', 
          message: 'Web Serial API در دسترس است',
          details: 'می‌توانید به دستگاه‌های سریال (RTL-SDR) متصل شوید'
        } : test
      ));
    } else {
      setTests(prev => prev.map((test, idx) => 
        idx === index ? { 
          ...test, 
          status: 'warning', 
          message: 'Web Serial API پشتیبانی نمی‌شود',
          details: 'برای اتصال به RTL-SDR از Chrome استفاده کنید'
        } : test
      ));
    }
  };

  const testSupabaseConnection = async (index: number) => {
    try {
      const response = await fetch('https://gkbwsukttbjsmvdxevzw.supabase.co/rest/v1/', {
        headers: {
          'apikey': 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImdrYndzdWt0dGJqc212ZHhldnp3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjcyMTYyOTAsImV4cCI6MjA4Mjc5MjI5MH0.Bg9ZdJAOwmtn6N4d7UP0fN9GGqJWZk2HApP8YIh7VAs'
        }
      });
      
      if (response.ok) {
        setTests(prev => prev.map((test, idx) => 
          idx === index ? { 
            ...test, 
            status: 'passed', 
            message: 'اتصال به Supabase برقرار است'
          } : test
        ));
      } else {
        throw new Error('Connection failed');
      }
    } catch (error) {
      setTests(prev => prev.map((test, idx) => 
        idx === index ? { 
          ...test, 
          status: 'failed', 
          message: 'خطا در اتصال به Supabase'
        } : test
      ));
    }
  };

  const testProcessingSpeed = async (index: number) => {
    const startTime = performance.now();
    
    let result = 0;
    for (let i = 0; i < 1000000; i++) {
      result += Math.sqrt(i) * Math.sin(i);
    }
    
    const duration = Math.round(performance.now() - startTime);
    
    setTests(prev => prev.map((test, idx) => 
      idx === index ? { 
        ...test, 
        status: duration < 500 ? 'passed' : duration < 1000 ? 'warning' : 'failed', 
        message: `سرعت پردازش: ${duration}ms`,
        details: duration < 500 ? 'عالی' : duration < 1000 ? 'متوسط' : 'کند'
      } : test
    ));
  };

  const resetTests = () => {
    setTests(tests.map(test => ({ ...test, status: 'pending', message: 'در انتظار تست...', duration: undefined, details: undefined })));
    setOverallProgress(0);
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'passed':
        return <CheckCircle className="w-5 h-5 text-green-600" />;
      case 'failed':
        return <XCircle className="w-5 h-5 text-red-600" />;
      case 'warning':
        return <AlertTriangle className="w-5 h-5 text-yellow-600" />;
      case 'running':
        return <Activity className="w-5 h-5 text-blue-600 animate-pulse" />;
      default:
        return <TestTube className="w-5 h-5 text-gray-400" />;
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'passed':
        return <Badge className="bg-green-100 text-green-800">موفق</Badge>;
      case 'failed':
        return <Badge className="bg-red-100 text-red-800">ناموفق</Badge>;
      case 'warning':
        return <Badge className="bg-yellow-100 text-yellow-800">هشدار</Badge>;
      case 'running':
        return <Badge className="bg-blue-100 text-blue-800 animate-pulse">در حال تست</Badge>;
      default:
        return <Badge className="bg-gray-100 text-gray-800">در انتظار</Badge>;
    }
  };

  return (
    <div className="space-y-6" dir="rtl">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 mb-1" style={{ fontFamily: 'BNazanin' }}>
            تست تجهیزات
          </h1>
          <p className="text-sm text-gray-600" style={{ fontFamily: 'BNazanin' }}>
            بررسی عملکرد تجهیزات و اطمینان از صحت عملکرد سیستم
          </p>
        </div>
        <div className="flex gap-2">
          <Button onClick={runAllTests} disabled={isRunning} className="access-button">
            <Play className="w-4 h-4 ml-2" />
            شروع تست‌ها
          </Button>
          <Button onClick={resetTests} variant="outline" disabled={isRunning}>
            <RotateCcw className="w-4 h-4 ml-2" />
            ریست
          </Button>
        </div>
      </div>

      {systemInfo && (
        <Card className="access-card bg-blue-50 border-blue-200">
          <CardHeader className="pb-2">
            <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <Cpu className="w-5 h-5 text-blue-600" />
              اطلاعات سیستم
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
              <div className="p-3 bg-white rounded-lg">
                <div className="text-gray-500" style={{ fontFamily: 'BNazanin' }}>مرورگر</div>
                <div className="font-medium">{systemInfo.browser}</div>
              </div>
              <div className="p-3 bg-white rounded-lg">
                <div className="text-gray-500" style={{ fontFamily: 'BNazanin' }}>پلتفرم</div>
                <div className="font-medium">{systemInfo.platform}</div>
              </div>
              <div className="p-3 bg-white rounded-lg">
                <div className="text-gray-500" style={{ fontFamily: 'BNazanin' }}>حافظه</div>
                <div className="font-medium">{systemInfo.memory}</div>
              </div>
              <div className="p-3 bg-white rounded-lg">
                <div className="text-gray-500" style={{ fontFamily: 'BNazanin' }}>هسته CPU</div>
                <div className="font-medium">{systemInfo.cores}</div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {isRunning && (
        <Card className="access-card border-blue-200">
          <CardContent className="pt-4">
            <div className="space-y-2">
              <div className="flex justify-between text-sm" style={{ fontFamily: 'BNazanin' }}>
                <span>پیشرفت کلی</span>
                <span>{Math.round(overallProgress)}%</span>
              </div>
              <Progress value={overallProgress} className="h-2" />
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {tests.map((test, index) => (
          <Card key={index} className={`access-card transition-all ${
            test.status === 'passed' ? 'border-green-200 bg-green-50/30' :
            test.status === 'failed' ? 'border-red-200 bg-red-50/30' :
            test.status === 'warning' ? 'border-yellow-200 bg-yellow-50/30' :
            test.status === 'running' ? 'border-blue-200 bg-blue-50/30' : ''
          }`}>
            <CardContent className="pt-4">
              <div className="flex items-start gap-3">
                <div className="mt-1">{getStatusIcon(test.status)}</div>
                <div className="flex-1">
                  <div className="flex items-center justify-between mb-1">
                    <h3 className="font-medium" style={{ fontFamily: 'BNazanin' }}>{test.name}</h3>
                    {getStatusBadge(test.status)}
                  </div>
                  <p className="text-sm text-gray-600" style={{ fontFamily: 'BNazanin' }}>{test.message}</p>
                  {test.details && (
                    <p className="text-xs text-gray-500 mt-1" style={{ fontFamily: 'BNazanin' }}>{test.details}</p>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card className="access-card">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
            <HardDrive className="w-5 h-5" />
            خلاصه نتایج
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-4 gap-4 text-center">
            <div className="p-4 bg-green-50 rounded-lg">
              <div className="text-2xl font-bold text-green-600">{tests.filter(t => t.status === 'passed').length}</div>
              <div className="text-sm text-green-700" style={{ fontFamily: 'BNazanin' }}>موفق</div>
            </div>
            <div className="p-4 bg-red-50 rounded-lg">
              <div className="text-2xl font-bold text-red-600">{tests.filter(t => t.status === 'failed').length}</div>
              <div className="text-sm text-red-700" style={{ fontFamily: 'BNazanin' }}>ناموفق</div>
            </div>
            <div className="p-4 bg-yellow-50 rounded-lg">
              <div className="text-2xl font-bold text-yellow-600">{tests.filter(t => t.status === 'warning').length}</div>
              <div className="text-sm text-yellow-700" style={{ fontFamily: 'BNazanin' }}>هشدار</div>
            </div>
            <div className="p-4 bg-gray-50 rounded-lg">
              <div className="text-2xl font-bold text-gray-600">{tests.filter(t => t.status === 'pending').length}</div>
              <div className="text-sm text-gray-700" style={{ fontFamily: 'BNazanin' }}>در انتظار</div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default EquipmentTest;
