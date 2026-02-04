import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { PieChart, Activity, Users, TrendingUp, MapPin, Cpu, Calendar, Loader2 } from 'lucide-react';
import { supabase } from '@/integrations/supabase/client';

interface DailyStats {
  date: string;
  count: number;
}

interface MinerTypeStats {
  type: string;
  count: number;
  percentage: number;
}

interface ProvinceStats {
  province: string;
  count: number;
  percentage: number;
}

const Statistics = () => {
  const [loading, setLoading] = useState(true);
  const [totalMiners, setTotalMiners] = useState(0);
  const [totalScans, setTotalScans] = useState(0);
  const [avgConfidence, setAvgConfidence] = useState(0);
  const [minerTypes, setMinerTypes] = useState<MinerTypeStats[]>([]);
  const [provinces, setProvinces] = useState<ProvinceStats[]>([]);
  const [dailyStats, setDailyStats] = useState<DailyStats[]>([]);
  const [recentActivity, setRecentActivity] = useState<any[]>([]);

  useEffect(() => {
    loadAllStatistics();
  }, []);

  const loadAllStatistics = async () => {
    setLoading(true);
    try {
      const { data: miners } = await supabase
        .from('detected_miners')
        .select('*');

      const { data: scans } = await supabase
        .from('scan_results')
        .select('*');

      const { data: logs } = await supabase
        .from('detection_logs')
        .select('*')
        .order('created_at', { ascending: false })
        .limit(10);

      if (miners) {
        setTotalMiners(miners.length);
        
        const totalConf = miners.reduce((sum, m) => sum + (m.confidence || 0), 0);
        setAvgConfidence(miners.length > 0 ? Math.round(totalConf / miners.length) : 0);

        const typeCount: Record<string, number> = {};
        const provinceCount: Record<string, number> = {};

        miners.forEach(m => {
          const type = m.miner_type || 'نامشخص';
          const prov = m.province || 'نامشخص';
          typeCount[type] = (typeCount[type] || 0) + 1;
          provinceCount[prov] = (provinceCount[prov] || 0) + 1;
        });

        setMinerTypes(
          Object.entries(typeCount)
            .map(([type, count]) => ({
              type,
              count,
              percentage: Math.round((count / miners.length) * 100)
            }))
            .sort((a, b) => b.count - a.count)
        );

        setProvinces(
          Object.entries(provinceCount)
            .map(([province, count]) => ({
              province,
              count,
              percentage: Math.round((count / miners.length) * 100)
            }))
            .sort((a, b) => b.count - a.count)
        );

        const dailyCount: Record<string, number> = {};
        miners.forEach(m => {
          const date = new Date(m.created_at).toLocaleDateString('fa-IR');
          dailyCount[date] = (dailyCount[date] || 0) + 1;
        });

        setDailyStats(
          Object.entries(dailyCount)
            .map(([date, count]) => ({ date, count }))
            .slice(-7)
        );
      }

      if (scans) {
        setTotalScans(scans.length);
      }

      if (logs) {
        setRecentActivity(logs);
      }
    } catch (error) {
      console.error('Error loading statistics:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Loader2 className="w-12 h-12 animate-spin text-blue-600" />
      </div>
    );
  }

  return (
    <div className="space-y-6" dir="rtl">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 mb-1" style={{ fontFamily: 'BNazanin' }}>
            آمار و تحلیل‌ها
          </h1>
          <p className="text-sm text-gray-600" style={{ fontFamily: 'BNazanin' }}>
            نمایش جامع آمار سیستم و ماینرهای شناسایی شده
          </p>
        </div>
        <Badge variant="outline" className="px-4 py-2">
          <PieChart className="w-4 h-4 ml-2" />
          آمار لحظه‌ای
        </Badge>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="access-card bg-gradient-to-br from-blue-50 to-blue-100 border-blue-200">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-blue-600" style={{ fontFamily: 'BNazanin' }}>کل ماینرها</p>
                <p className="text-3xl font-bold text-blue-800">{totalMiners}</p>
              </div>
              <div className="p-3 bg-blue-200 rounded-full">
                <Cpu className="w-6 h-6 text-blue-700" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="access-card bg-gradient-to-br from-green-50 to-green-100 border-green-200">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-green-600" style={{ fontFamily: 'BNazanin' }}>تعداد اسکن‌ها</p>
                <p className="text-3xl font-bold text-green-800">{totalScans}</p>
              </div>
              <div className="p-3 bg-green-200 rounded-full">
                <Activity className="w-6 h-6 text-green-700" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="access-card bg-gradient-to-br from-purple-50 to-purple-100 border-purple-200">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-purple-600" style={{ fontFamily: 'BNazanin' }}>میانگین اطمینان</p>
                <p className="text-3xl font-bold text-purple-800">{avgConfidence}%</p>
              </div>
              <div className="p-3 bg-purple-200 rounded-full">
                <TrendingUp className="w-6 h-6 text-purple-700" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="access-card bg-gradient-to-br from-orange-50 to-orange-100 border-orange-200">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-orange-600" style={{ fontFamily: 'BNazanin' }}>تعداد استان‌ها</p>
                <p className="text-3xl font-bold text-orange-800">{provinces.length}</p>
              </div>
              <div className="p-3 bg-orange-200 rounded-full">
                <MapPin className="w-6 h-6 text-orange-700" />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="overview" className="space-y-6">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview" style={{ fontFamily: 'BNazanin' }}>نمای کلی</TabsTrigger>
          <TabsTrigger value="types" style={{ fontFamily: 'BNazanin' }}>انواع ماینر</TabsTrigger>
          <TabsTrigger value="locations" style={{ fontFamily: 'BNazanin' }}>موقعیت‌ها</TabsTrigger>
          <TabsTrigger value="activity" style={{ fontFamily: 'BNazanin' }}>فعالیت اخیر</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card className="access-card">
              <CardHeader>
                <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                  <Calendar className="w-5 h-5" />
                  آمار روزانه (۷ روز اخیر)
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {dailyStats.length === 0 ? (
                    <p className="text-center text-gray-500" style={{ fontFamily: 'BNazanin' }}>داده‌ای موجود نیست</p>
                  ) : (
                    dailyStats.map((day, idx) => (
                      <div key={idx} className="flex items-center gap-4">
                        <span className="w-24 text-sm text-gray-600" style={{ fontFamily: 'BNazanin' }}>{day.date}</span>
                        <div className="flex-1">
                          <Progress value={(day.count / Math.max(...dailyStats.map(d => d.count))) * 100} className="h-3" />
                        </div>
                        <span className="w-12 text-sm font-bold text-left">{day.count}</span>
                      </div>
                    ))
                  )}
                </div>
              </CardContent>
            </Card>

            <Card className="access-card">
              <CardHeader>
                <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                  <PieChart className="w-5 h-5" />
                  توزیع کلی
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-2 gap-4">
                  <div className="p-4 bg-blue-50 rounded-lg text-center">
                    <div className="text-2xl font-bold text-blue-700">{minerTypes.length}</div>
                    <div className="text-sm text-blue-600" style={{ fontFamily: 'BNazanin' }}>نوع ماینر</div>
                  </div>
                  <div className="p-4 bg-green-50 rounded-lg text-center">
                    <div className="text-2xl font-bold text-green-700">{provinces.length}</div>
                    <div className="text-sm text-green-600" style={{ fontFamily: 'BNazanin' }}>استان</div>
                  </div>
                  <div className="p-4 bg-purple-50 rounded-lg text-center">
                    <div className="text-2xl font-bold text-purple-700">{totalScans}</div>
                    <div className="text-sm text-purple-600" style={{ fontFamily: 'BNazanin' }}>اسکن انجام شده</div>
                  </div>
                  <div className="p-4 bg-orange-50 rounded-lg text-center">
                    <div className="text-2xl font-bold text-orange-700">{avgConfidence}%</div>
                    <div className="text-sm text-orange-600" style={{ fontFamily: 'BNazanin' }}>دقت میانگین</div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="types" className="space-y-4">
          <Card className="access-card">
            <CardHeader>
              <CardTitle className="text-lg" style={{ fontFamily: 'BNazanin' }}>توزیع بر اساس نوع ماینر</CardTitle>
            </CardHeader>
            <CardContent>
              {minerTypes.length === 0 ? (
                <p className="text-center text-gray-500 py-8" style={{ fontFamily: 'BNazanin' }}>داده‌ای موجود نیست</p>
              ) : (
                <div className="space-y-4">
                  {minerTypes.map((item, idx) => (
                    <div key={idx} className="space-y-2">
                      <div className="flex justify-between items-center">
                        <span className="font-medium" style={{ fontFamily: 'BNazanin' }}>{item.type}</span>
                        <div className="flex items-center gap-2">
                          <Badge variant="outline">{item.count}</Badge>
                          <span className="text-sm text-gray-500">{item.percentage}%</span>
                        </div>
                      </div>
                      <Progress value={item.percentage} className="h-3" />
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="locations" className="space-y-4">
          <Card className="access-card">
            <CardHeader>
              <CardTitle className="text-lg" style={{ fontFamily: 'BNazanin' }}>توزیع بر اساس استان</CardTitle>
            </CardHeader>
            <CardContent>
              {provinces.length === 0 ? (
                <p className="text-center text-gray-500 py-8" style={{ fontFamily: 'BNazanin' }}>داده‌ای موجود نیست</p>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {provinces.map((item, idx) => (
                    <div key={idx} className="p-4 border rounded-lg hover:bg-gray-50 transition-colors">
                      <div className="flex justify-between items-center mb-2">
                        <span className="font-medium flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                          <MapPin className="w-4 h-4 text-blue-500" />
                          {item.province}
                        </span>
                        <Badge>{item.count}</Badge>
                      </div>
                      <Progress value={item.percentage} className="h-2" />
                      <p className="text-xs text-gray-500 mt-1 text-left">{item.percentage}% از کل</p>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="activity" className="space-y-4">
          <Card className="access-card">
            <CardHeader>
              <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                <Activity className="w-5 h-5" />
                فعالیت‌های اخیر
              </CardTitle>
            </CardHeader>
            <CardContent>
              {recentActivity.length === 0 ? (
                <p className="text-center text-gray-500 py-8" style={{ fontFamily: 'BNazanin' }}>فعالیتی ثبت نشده است</p>
              ) : (
                <div className="space-y-3">
                  {recentActivity.map((item, idx) => (
                    <div key={idx} className="flex items-center gap-4 p-3 border rounded-lg">
                      <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                      <div className="flex-1">
                        <p className="text-sm" style={{ fontFamily: 'BNazanin' }}>{item.scan_type || 'اسکن'}</p>
                        <p className="text-xs text-gray-500">
                          {new Date(item.created_at).toLocaleString('fa-IR')}
                        </p>
                      </div>
                      <Badge variant="outline">{item.status || 'completed'}</Badge>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default Statistics;
