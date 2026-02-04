
import React, { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { MapPin, Navigation } from 'lucide-react';

interface DatabaseRecord {
  id: string;
  deviceName: string;
  location: string;
  detectionDate: string;
  method: string;
  confidence: number;
  status: string;
}

const Database = () => {
  const [searchTerm, setSearchTerm] = useState('');
  
  const records: DatabaseRecord[] = [
    {
      id: '1',
      deviceName: 'Antminer S19 Pro',
      location: 'تهران، پونک',
      detectionDate: '1402/12/15',
      method: 'تحلیل شبکه',
      confidence: 94,
      status: 'فعال'
    },
    {
      id: '2',
      deviceName: 'Whatsminer M30S',
      location: 'تهران، ونک',
      detectionDate: '1402/12/14',
      method: 'اسکن RF',
      confidence: 87,
      status: 'تایید شده'
    },
    {
      id: '3',
      deviceName: 'Unknown ASIC',
      location: 'تهران، نارمک',
      detectionDate: '1402/12/13',
      method: 'تحلیل حرارتی',
      confidence: 73,
      status: 'مشکوک'
    }
  ];

  const filteredRecords = records.filter(record =>
    record.deviceName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    record.location.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="space-y-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground mb-2">
          پایگاه داده محلی
        </h1>
        <p className="text-muted-foreground">
          مدیریت و ذخیره‌سازی اطلاعات دستگاه‌های شناسایی شده
        </p>
      </div>

      <Tabs defaultValue="records" className="space-y-6">
        <TabsList className="grid w-full grid-cols-3 bg-secondary">
          <TabsTrigger value="records">رکوردهای ذخیره شده</TabsTrigger>
          <TabsTrigger value="analytics">آمار و تحلیل</TabsTrigger>
          <TabsTrigger value="export">خروجی و پشتیبان</TabsTrigger>
        </TabsList>

        {/* Records Tab */}
        <TabsContent value="records" className="space-y-6">
          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <MapPin className="w-5 h-5 text-primary" />
                جستجو در رکوردها
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <Input
                  placeholder="جستجو بر اساس نام دستگاه یا مکان..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-full"
                />
                
                <div className="text-sm text-muted-foreground">
                  {filteredRecords.length} رکورد یافت شد
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle>فهرست رکوردها</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {filteredRecords.map((record) => (
                  <div key={record.id} className="p-4 bg-secondary/30 rounded-lg border border-border">
                    <div className="flex items-center justify-between">
                      <div className="space-y-1">
                        <h4 className="font-medium text-foreground">{record.deviceName}</h4>
                        <p className="text-sm text-muted-foreground">{record.location}</p>
                        <div className="flex items-center gap-4 text-xs text-muted-foreground">
                          <span>تاریخ: {record.detectionDate}</span>
                          <span>روش: {record.method}</span>
                          <span>اطمینان: {record.confidence}%</span>
                        </div>
                      </div>
                      
                      <div className="flex items-center gap-2">
                        <Badge 
                          variant={record.status === 'فعال' ? 'default' : 'outline'}
                          className={record.status === 'فعال' ? 'bg-accent text-accent-foreground' : ''}
                        >
                          {record.status}
                        </Badge>
                        <Button size="sm" variant="outline">
                          ویرایش
                        </Button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Analytics Tab */}
        <TabsContent value="analytics" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <Card className="bg-card/50 backdrop-blur-sm border-border">
              <CardContent className="p-4 text-center">
                <div className="text-2xl font-bold text-primary">247</div>
                <div className="text-sm text-muted-foreground">کل رکوردها</div>
              </CardContent>
            </Card>
            
            <Card className="bg-card/50 backdrop-blur-sm border-border">
              <CardContent className="p-4 text-center">
                <div className="text-2xl font-bold text-accent">89</div>
                <div className="text-sm text-muted-foreground">تایید شده</div>
              </CardContent>
            </Card>
            
            <Card className="bg-card/50 backdrop-blur-sm border-border">
              <CardContent className="p-4 text-center">
                <div className="text-2xl font-bold text-destructive">23</div>
                <div className="text-sm text-muted-foreground">مشکوک</div>
              </CardContent>
            </Card>
            
            <Card className="bg-card/50 backdrop-blur-sm border-border">
              <CardContent className="p-4 text-center">
                <div className="text-2xl font-bold text-foreground">91%</div>
                <div className="text-sm text-muted-foreground">دقت میانگین</div>
              </CardContent>
            </Card>
          </div>

          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle>آمار روش‌های تشخیص</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                <div className="flex items-center justify-between p-3 bg-primary/10 rounded-lg">
                  <span>تحلیل شبکه</span>
                  <span className="font-medium text-primary">45%</span>
                </div>
                <div className="flex items-center justify-between p-3 bg-accent/10 rounded-lg">
                  <span>اسکن RF</span>
                  <span className="font-medium text-accent">28%</span>
                </div>
                <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                  <span>تحلیل حرارتی</span>
                  <span className="font-medium">18%</span>
                </div>
                <div className="flex items-center justify-between p-3 bg-muted/20 rounded-lg">
                  <span>سایر روش‌ها</span>
                  <span className="font-medium text-muted-foreground">9%</span>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Export Tab */}
        <TabsContent value="export" className="space-y-6">
          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Navigation className="w-5 h-5 text-primary" />
                خروجی داده‌ها
              </CardTitle>
              <CardDescription>
                تهیه فایل از اطلاعات ذخیره شده
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <Button variant="outline" className="h-20 flex flex-col gap-2">
                  <span>خروجی Excel</span>
                  <span className="text-xs text-muted-foreground">فرمت .xlsx</span>
                </Button>
                <Button variant="outline" className="h-20 flex flex-col gap-2">
                  <span>خروجی CSV</span>
                  <span className="text-xs text-muted-foreground">فرمت .csv</span>
                </Button>
                <Button variant="outline" className="h-20 flex flex-col gap-2">
                  <span>خروجی JSON</span>
                  <span className="text-xs text-muted-foreground">فرمت .json</span>
                </Button>
                <Button variant="outline" className="h-20 flex flex-col gap-2">
                  <span>گزارش PDF</span>
                  <span className="text-xs text-muted-foreground">فرمت .pdf</span>
                </Button>
              </div>
            </CardContent>
          </Card>

          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle>پشتیبان‌گیری</CardTitle>
              <CardDescription>
                ایجاد کپی احتیاطی از پایگاه داده
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex gap-4">
                <Button className="bg-primary hover:bg-primary/90">
                  ایجاد پشتیبان کامل
                </Button>
                <Button variant="outline">
                  بازیابی از پشتیبان
                </Button>
              </div>
              
              <div className="p-4 bg-muted/20 rounded-lg">
                <p className="text-sm text-muted-foreground">
                  آخرین پشتیبان‌گیری: ۱۴۰۲/۱۲/۱۵ - ۱۴:۳۰
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default Database;
