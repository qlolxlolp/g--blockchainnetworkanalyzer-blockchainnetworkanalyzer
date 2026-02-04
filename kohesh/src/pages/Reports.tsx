import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { FileText, BarChart, TrendingUp, Download, Calendar, Loader2, Printer, Share2, FileSpreadsheet, FileJson } from 'lucide-react';
import { useToast } from '@/hooks/use-toast';
import { supabase } from '@/integrations/supabase/client';

interface ReportData {
  id: string;
  title: string;
  report_type: string;
  content: any;
  summary: string;
  generated_at: string;
  status: string;
}

interface MinerStats {
  total: number;
  byType: { type: string; count: number }[];
  byProvince: { province: string; count: number }[];
  byConfidence: { range: string; count: number }[];
}

const Reports = () => {
  const { toast } = useToast();
  const [loading, setLoading] = useState(false);
  const [reports, setReports] = useState<ReportData[]>([]);
  const [stats, setStats] = useState<MinerStats>({ total: 0, byType: [], byProvince: [], byConfidence: [] });
  const [selectedPeriod, setSelectedPeriod] = useState('week');
  const [generatingReport, setGeneratingReport] = useState(false);

  useEffect(() => {
    loadReports();
    loadStats();
  }, []);

  const loadReports = async () => {
    setLoading(true);
    try {
      const { data, error } = await supabase
        .from('reports')
        .select('*')
        .order('generated_at', { ascending: false })
        .limit(10);

      if (error) throw error;
      setReports(data || []);
    } catch (error) {
      console.error('Error loading reports:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadStats = async () => {
    try {
      const { data: miners, error } = await supabase
        .from('detected_miners')
        .select('miner_type, province, confidence');

      if (error) throw error;

      if (miners) {
        const byType: Record<string, number> = {};
        const byProvince: Record<string, number> = {};
        const byConfidence: Record<string, number> = { 'بالای ۹۰٪': 0, '۷۰-۹۰٪': 0, 'زیر ۷۰٪': 0 };

        miners.forEach(m => {
          byType[m.miner_type || 'نامشخص'] = (byType[m.miner_type || 'نامشخص'] || 0) + 1;
          byProvince[m.province || 'نامشخص'] = (byProvince[m.province || 'نامشخص'] || 0) + 1;
          
          const conf = m.confidence || 0;
          if (conf >= 90) byConfidence['بالای ۹۰٪']++;
          else if (conf >= 70) byConfidence['۷۰-۹۰٪']++;
          else byConfidence['زیر ۷۰٪']++;
        });

        setStats({
          total: miners.length,
          byType: Object.entries(byType).map(([type, count]) => ({ type, count })),
          byProvince: Object.entries(byProvince).map(([province, count]) => ({ province, count })),
          byConfidence: Object.entries(byConfidence).map(([range, count]) => ({ range, count }))
        });
      }
    } catch (error) {
      console.error('Error loading stats:', error);
    }
  };

  const generateReport = async (type: string) => {
    setGeneratingReport(true);
    try {
      const reportContent = {
        generatedAt: new Date().toISOString(),
        period: selectedPeriod,
        statistics: stats,
        detectedMiners: stats.total,
        reportType: type
      };

      const { error } = await supabase.from('reports').insert({
        title: `گزارش ${type === 'summary' ? 'خلاصه' : type === 'detailed' ? 'تفصیلی' : 'روند'} - ${new Date().toLocaleDateString('fa-IR')}`,
        report_type: type,
        content: reportContent,
        summary: `گزارش شامل ${stats.total} ماینر شناسایی شده`,
        status: 'completed'
      });

      if (error) throw error;

      toast({ title: "گزارش ایجاد شد", description: "گزارش با موفقیت ذخیره شد" });
      loadReports();
    } catch (error: any) {
      toast({ title: "خطا", description: error.message, variant: "destructive" });
    } finally {
      setGeneratingReport(false);
    }
  };

  const exportReport = (format: string, report?: ReportData) => {
    const data = report?.content || {
      title: 'گزارش ماینرهای شناسایی شده',
      date: new Date().toLocaleDateString('fa-IR'),
      stats: stats
    };

    let content = '';
    let filename = `report_${Date.now()}`;
    let mimeType = '';

    switch (format) {
      case 'json':
        content = JSON.stringify(data, null, 2);
        filename += '.json';
        mimeType = 'application/json';
        break;
      case 'csv':
        content = 'نوع,تعداد\n';
        stats.byType.forEach(item => {
          content += `${item.type},${item.count}\n`;
        });
        filename += '.csv';
        mimeType = 'text/csv';
        break;
      default:
        content = JSON.stringify(data, null, 2);
        filename += '.txt';
        mimeType = 'text/plain';
    }

    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);

    toast({ title: "دانلود شد", description: `فایل ${format.toUpperCase()} دانلود شد` });
  };

  return (
    <div className="space-y-6" dir="rtl">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 mb-1" style={{ fontFamily: 'BNazanin' }}>
            گزارشات و تحلیل‌ها
          </h1>
          <p className="text-sm text-gray-600" style={{ fontFamily: 'BNazanin' }}>
            تولید گزارشات جامع از عملکرد سیستم و ماینرهای شناسایی شده
          </p>
        </div>
        <Badge variant="outline" className="px-4 py-2">
          <FileText className="w-4 h-4 ml-2" />
          {stats.total} ماینر ثبت شده
        </Badge>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="access-card bg-blue-50 border-blue-200">
          <CardContent className="pt-4 text-center">
            <div className="text-3xl font-bold text-blue-700">{stats.total}</div>
            <div className="text-sm text-blue-600" style={{ fontFamily: 'BNazanin' }}>کل ماینرها</div>
          </CardContent>
        </Card>
        <Card className="access-card bg-green-50 border-green-200">
          <CardContent className="pt-4 text-center">
            <div className="text-3xl font-bold text-green-700">{stats.byType.length}</div>
            <div className="text-sm text-green-600" style={{ fontFamily: 'BNazanin' }}>نوع دستگاه</div>
          </CardContent>
        </Card>
        <Card className="access-card bg-purple-50 border-purple-200">
          <CardContent className="pt-4 text-center">
            <div className="text-3xl font-bold text-purple-700">{stats.byProvince.length}</div>
            <div className="text-sm text-purple-600" style={{ fontFamily: 'BNazanin' }}>استان</div>
          </CardContent>
        </Card>
        <Card className="access-card bg-orange-50 border-orange-200">
          <CardContent className="pt-4 text-center">
            <div className="text-3xl font-bold text-orange-700">{reports.length}</div>
            <div className="text-sm text-orange-600" style={{ fontFamily: 'BNazanin' }}>گزارش تولید شده</div>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="generate" className="space-y-6">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="generate" style={{ fontFamily: 'BNazanin' }}>تولید گزارش</TabsTrigger>
          <TabsTrigger value="history" style={{ fontFamily: 'BNazanin' }}>تاریخچه گزارشات</TabsTrigger>
          <TabsTrigger value="export" style={{ fontFamily: 'BNazanin' }}>خروجی داده</TabsTrigger>
        </TabsList>

        <TabsContent value="generate" className="space-y-6">
          <Card className="access-card">
            <CardHeader>
              <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                <BarChart className="w-5 h-5" />
                تولید گزارش جدید
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-4">
                <div className="flex-1">
                  <label className="text-sm text-gray-600 mb-2 block" style={{ fontFamily: 'BNazanin' }}>دوره زمانی</label>
                  <Select value={selectedPeriod} onValueChange={setSelectedPeriod}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="day">امروز</SelectItem>
                      <SelectItem value="week">هفته گذشته</SelectItem>
                      <SelectItem value="month">ماه گذشته</SelectItem>
                      <SelectItem value="all">همه زمان‌ها</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <Button
                  onClick={() => generateReport('summary')}
                  disabled={generatingReport}
                  className="h-24 flex flex-col items-center justify-center gap-2 access-button"
                >
                  {generatingReport ? <Loader2 className="w-6 h-6 animate-spin" /> : <FileText className="w-6 h-6" />}
                  <span style={{ fontFamily: 'BNazanin' }}>گزارش خلاصه</span>
                </Button>
                <Button
                  onClick={() => generateReport('detailed')}
                  disabled={generatingReport}
                  className="h-24 flex flex-col items-center justify-center gap-2 access-button"
                >
                  {generatingReport ? <Loader2 className="w-6 h-6 animate-spin" /> : <BarChart className="w-6 h-6" />}
                  <span style={{ fontFamily: 'BNazanin' }}>گزارش تفصیلی</span>
                </Button>
                <Button
                  onClick={() => generateReport('trend')}
                  disabled={generatingReport}
                  className="h-24 flex flex-col items-center justify-center gap-2 access-button"
                >
                  {generatingReport ? <Loader2 className="w-6 h-6 animate-spin" /> : <TrendingUp className="w-6 h-6" />}
                  <span style={{ fontFamily: 'BNazanin' }}>تحلیل روند</span>
                </Button>
              </div>
            </CardContent>
          </Card>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card className="access-card">
              <CardHeader>
                <CardTitle className="text-base" style={{ fontFamily: 'BNazanin' }}>توزیع بر اساس نوع</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {stats.byType.slice(0, 5).map((item, idx) => (
                  <div key={idx} className="space-y-1">
                    <div className="flex justify-between text-sm">
                      <span style={{ fontFamily: 'BNazanin' }}>{item.type}</span>
                      <span className="font-bold">{item.count}</span>
                    </div>
                    <Progress value={(item.count / stats.total) * 100} className="h-2" />
                  </div>
                ))}
              </CardContent>
            </Card>

            <Card className="access-card">
              <CardHeader>
                <CardTitle className="text-base" style={{ fontFamily: 'BNazanin' }}>توزیع بر اساس سطح اطمینان</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {stats.byConfidence.map((item, idx) => (
                  <div key={idx} className="space-y-1">
                    <div className="flex justify-between text-sm">
                      <span style={{ fontFamily: 'BNazanin' }}>{item.range}</span>
                      <span className="font-bold">{item.count}</span>
                    </div>
                    <Progress 
                      value={(item.count / Math.max(stats.total, 1)) * 100} 
                      className={`h-2 ${idx === 0 ? 'bg-green-100' : idx === 1 ? 'bg-yellow-100' : 'bg-red-100'}`} 
                    />
                  </div>
                ))}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="history" className="space-y-4">
          <Card className="access-card">
            <CardHeader>
              <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                <Calendar className="w-5 h-5" />
                گزارشات اخیر
              </CardTitle>
            </CardHeader>
            <CardContent>
              {loading ? (
                <div className="flex items-center justify-center py-8">
                  <Loader2 className="w-8 h-8 animate-spin text-blue-500" />
                </div>
              ) : reports.length === 0 ? (
                <div className="text-center py-8 text-gray-500" style={{ fontFamily: 'BNazanin' }}>
                  هنوز گزارشی ایجاد نشده است
                </div>
              ) : (
                <div className="space-y-3">
                  {reports.map((report) => (
                    <div key={report.id} className="p-4 border rounded-lg hover:bg-gray-50 transition-colors">
                      <div className="flex items-center justify-between">
                        <div>
                          <h4 className="font-medium" style={{ fontFamily: 'BNazanin' }}>{report.title}</h4>
                          <p className="text-sm text-gray-500">{report.summary}</p>
                          <p className="text-xs text-gray-400 mt-1">
                            {new Date(report.generated_at).toLocaleDateString('fa-IR')}
                          </p>
                        </div>
                        <div className="flex items-center gap-2">
                          <Badge variant="outline">{report.report_type}</Badge>
                          <Button size="sm" variant="outline" onClick={() => exportReport('json', report)}>
                            <Download className="w-4 h-4" />
                          </Button>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="export" className="space-y-4">
          <Card className="access-card">
            <CardHeader>
              <CardTitle className="text-lg flex items-center gap-2" style={{ fontFamily: 'BNazanin' }}>
                <Download className="w-5 h-5" />
                خروجی گرفتن از داده‌ها
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <Button
                  variant="outline"
                  className="h-24 flex flex-col items-center justify-center gap-2"
                  onClick={() => exportReport('json')}
                >
                  <FileJson className="w-8 h-8 text-blue-600" />
                  <span style={{ fontFamily: 'BNazanin' }}>JSON</span>
                </Button>
                <Button
                  variant="outline"
                  className="h-24 flex flex-col items-center justify-center gap-2"
                  onClick={() => exportReport('csv')}
                >
                  <FileSpreadsheet className="w-8 h-8 text-green-600" />
                  <span style={{ fontFamily: 'BNazanin' }}>CSV</span>
                </Button>
                <Button
                  variant="outline"
                  className="h-24 flex flex-col items-center justify-center gap-2"
                  onClick={() => window.print()}
                >
                  <Printer className="w-8 h-8 text-purple-600" />
                  <span style={{ fontFamily: 'BNazanin' }}>چاپ</span>
                </Button>
                <Button
                  variant="outline"
                  className="h-24 flex flex-col items-center justify-center gap-2"
                  onClick={() => {
                    navigator.share?.({
                      title: 'گزارش ماینرها',
                      text: `تعداد ماینرهای شناسایی شده: ${stats.total}`
                    });
                  }}
                >
                  <Share2 className="w-8 h-8 text-orange-600" />
                  <span style={{ fontFamily: 'BNazanin' }}>اشتراک‌گذاری</span>
                </Button>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default Reports;
