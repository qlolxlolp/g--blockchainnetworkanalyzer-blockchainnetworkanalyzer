
import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Brain, Cpu, Zap } from 'lucide-react';

const MachineLearning = () => {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-normal text-foreground" style={{ fontFamily: 'BNazanin' }}>
          یادگیری ماشین
        </h1>
        <div className="flex items-center gap-2 px-4 py-2 bg-primary/10 rounded-lg">
          <Brain className="w-4 h-4 text-primary" />
          <span className="text-sm text-primary">مدل‌ها آماده</span>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="bg-card/50 backdrop-blur-sm border-border">
          <CardHeader>
            <CardTitle className="text-lg font-normal flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <Cpu className="w-5 h-5 text-primary" />
              مدل‌های آموزش دیده
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground" style={{ fontFamily: 'BNazanin' }}>
              مدل‌های یادگیری ماشین برای تشخیص الگوهای ماینر و بهبود دقت شناسایی
            </p>
          </CardContent>
        </Card>

        <Card className="bg-card/50 backdrop-blur-sm border-border">
          <CardHeader>
            <CardTitle className="text-lg font-normal flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <Zap className="w-5 h-5 text-primary" />
              پردازش هوشمند
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground" style={{ fontFamily: 'BNazanin' }}>
              الگوریتم‌های پیشرفته برای تحلیل داده‌ها و پیش‌بینی رفتار ماینرها
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default MachineLearning;
