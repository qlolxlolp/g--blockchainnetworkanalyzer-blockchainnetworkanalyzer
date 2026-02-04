
import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Eye, Search, Target } from 'lucide-react';

const PatternRecognition = () => {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-normal text-foreground" style={{ fontFamily: 'BNazanin' }}>
          تشخیص الگو
        </h1>
        <div className="flex items-center gap-2 px-4 py-2 bg-primary/10 rounded-lg">
          <Eye className="w-4 h-4 text-primary" />
          <span className="text-sm text-primary">الگویابی فعال</span>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="bg-card/50 backdrop-blur-sm border-border">
          <CardHeader>
            <CardTitle className="text-lg font-normal flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <Search className="w-5 h-5 text-primary" />
              تحلیل الگوی سیگنال
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground" style={{ fontFamily: 'BNazanin' }}>
              شناسایی الگوهای منحصربفرد سیگنال‌های ماینر برای تشخیص دقیق‌تر
            </p>
          </CardContent>
        </Card>

        <Card className="bg-card/50 backdrop-blur-sm border-border">
          <CardHeader>
            <CardTitle className="text-lg font-normal flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
              <Target className="w-5 h-5 text-primary" />
              تطبیق الگو
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground" style={{ fontFamily: 'BNazanin' }}>
              مقایسه سیگنال‌های دریافتی با پایگاه داده الگوهای شناخته شده
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default PatternRecognition;
