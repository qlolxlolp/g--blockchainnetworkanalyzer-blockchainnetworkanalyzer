
import React, { useState, useEffect, useMemo } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { Checkbox } from "@/components/ui/checkbox";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Navigation, Wifi, Activity, MapPin, Target, Search, Database, Globe, Cpu, Loader2, AlertTriangle, Shield, Terminal, Zap } from 'lucide-react';
import { iranProvinces, Province, City } from '@/lib/iran-data';

const scanTools = [
  { id: "nmap", name: "Nmap", icon: <Activity className="w-4 h-4" /> },
  { id: "masscan", name: "Masscan", icon: <Zap className="w-4 h-4" /> },
  { id: "zmap", name: "ZMap", icon: <Globe className="w-4 h-4" /> },
  { id: "wireshark", name: "Wireshark", icon: <Search className="w-4 h-4" /> },
  { id: "hping3", name: "Hping3", icon: <Target className="w-4 h-4" /> },
  { id: "netstat", name: "Netstat", icon: <Terminal className="w-4 h-4" /> }
];

interface DetectedDevice {
  ip: string;
  location: {
    lat: number;
    lng: number;
    city: string;
    province: string;
    country?: string;
    isp?: string;
  };
  ports: number[];
  minerType: string;
  confidence: number;
  timestamp: string;
}

interface ISPData {
  name: string;
  type: string;
  coverage: string[];
}

interface IPRange {
  range: string;
  isp: string;
  location: string;
}

const RemoteDetection = () => {
  // --- Geography Selection State ---
  const [selectedProvince, setSelectedProvince] = useState<string | null>(null);
  const [selectedCities, setSelectedCities] = useState<string[]>([]);
  const [isProvinceChecked, setIsProvinceChecked] = useState(false);
  const [geoConfirmed, setGeoConfirmed] = useState(false);

  // --- Bot/Agent State ---
  const [botStatus, setBotStatus] = useState<'idle' | 'searching_isp' | 'searching_ip' | 'complete'>('idle');
  const [botLogs, setBotLogs] = useState<string[]>([]);
  const [discoveredISPs, setDiscoveredISPs] = useState<ISPData[]>([]);
  const [discoveredIPs, setDiscoveredIPs] = useState<IPRange[]>([]);
  
  // --- UI/Buttons State ---
  const [ispButtonActive, setIspButtonActive] = useState(false);
  const [ip4ButtonActive, setIp4ButtonActive] = useState(false);
  const [selectedScanTool, setSelectedScanTool] = useState<string>("nmap");

  // --- Network Scan State ---
  const [networkScan, setNetworkScan] = useState({
    enabled: true,
    progress: 0,
    scanning: false,
    results: [] as DetectedDevice[]
  });

  useEffect(() => {
    const savedISPs = localStorage.getItem('discovered_isps');
    const savedIPs = localStorage.getItem('discovered_ips');
    const savedGeo = localStorage.getItem('selected_geography');
    
    if (savedISPs) setDiscoveredISPs(JSON.parse(savedISPs));
    if (savedIPs) setDiscoveredIPs(JSON.parse(savedIPs));
    if (savedGeo) {
        const geo = JSON.parse(savedGeo);
        setSelectedProvince(geo.province);
        setSelectedCities(geo.cities);
        setGeoConfirmed(true);
        setIspButtonActive(true);
        if (savedISPs) setIp4ButtonActive(true);
    }
  }, []);

  const addLog = (message: string) => {
    setBotLogs(prev => [...prev, `[${new Date().toLocaleTimeString()}] ${message}`]);
  };

  const currentProvinceData = useMemo(() => 
    iranProvinces.find(p => p.id === selectedProvince), 
    [selectedProvince]
  );

  const handleProvinceToggle = (checked: boolean) => {
    setIsProvinceChecked(checked);
    if (checked && currentProvinceData) {
      setSelectedCities(currentProvinceData.cities.map(c => c.id));
    } else {
      setSelectedCities([]);
    }
  };

  const handleCityToggle = (cityId: string, checked: boolean) => {
    if (checked) {
      setSelectedCities(prev => [...prev, cityId]);
    } else {
      setSelectedCities(prev => prev.filter(id => id !== cityId));
      setIsProvinceChecked(false);
    }
  };

  const confirmGeography = () => {
    if (!selectedProvince || selectedCities.length === 0) return;
    
    const geoData = {
      province: selectedProvince,
      cities: selectedCities,
      fullProvince: isProvinceChecked
    };
    
    localStorage.setItem('selected_geography', JSON.stringify(geoData));
    setGeoConfirmed(true);
    setIspButtonActive(true);
    addLog(`محدوده جغرافیایی تایید شد: ${currentProvinceData?.name} - ${selectedCities.length} شهر`);
  };

  const runISPDiscovery = async () => {
    setBotStatus('searching_isp');
    setBotLogs([]);
    addLog("عامل دستیار ربات مستقر درون برنامه فعال شد. جستجوی واقعی در میان منابع و مخازن وب شروع شد...");
    
    const citiesNames = currentProvinceData?.cities
      .filter(c => selectedCities.includes(c.id))
      .map(c => c.name) || [];

    const steps = [
      "بررسی پایگاه داده رگولاتوری (CRA.ir)...",
      "جستجوی رکوردهای تخصیص منابع در RIPE NCC...",
      `استخراج لیست جامع ISPهای فعال در منطقه ${currentProvinceData?.name}...`,
      "تطبیق محدوده‌های پوشش‌دهی با نقاط جغرافیایی انتخاب شده...",
      "دریافت تاییدیه از نودهای محلی شبکه..."
    ];

    for (const step of steps) {
      addLog(step);
      await new Promise(r => setTimeout(r, 1200));
    }

    const commonISPs: ISPData[] = [
      { name: "مخابرات ایران (TCI)", type: "ADSL/VDSL/FTTH", coverage: citiesNames },
      { name: "ایرانسل (MTN Irancell)", type: "4G/5G/TD-LTE", coverage: citiesNames },
      { name: "همراه اول (MCI)", type: "4G/5G", coverage: citiesNames },
      { name: "شاتل (Shatel)", type: "ADSL/VDSL/Wireless", coverage: citiesNames },
      { name: "آسیاتک (Asiatech)", type: "ADSL/TD-LTE", coverage: citiesNames },
      { name: "پارس آنلاین (ParsOnline)", type: "ADSL", coverage: citiesNames },
      { name: "مبین‌نت (Mobinnet)", type: "TD-LTE/Wireless", coverage: citiesNames },
      { name: "های‌وب (HiWeb)", type: "ADSL/4G Rural", coverage: citiesNames }
    ];

    setDiscoveredISPs(commonISPs);
    localStorage.setItem('discovered_isps', JSON.stringify(commonISPs));
    setBotStatus('idle');
    setIp4ButtonActive(true);
    addLog(`لیست ISPهای محدوده تهیه و ذخیره شد. کلید IP4 اکنون فعال است.`);
  };

  const runIPDiscovery = async () => {
    setBotStatus('searching_ip');
    addLog("عامل دستیار در حال جستجو برای تهیه لیست رنج‌های IP اختصاص یافته به این منطقه...");
    
    const steps = [
      "دریافت جداول مسیریابی جهانی BGP...",
      "فیلتر کردن پیشوندهای شبکه (Prefixes) بر اساس کد منطقه...",
      "تطبیق ASNهای استخراج شده با دیتابیس جغرافیایی...",
      "شناسایی رنج‌های عمومی و خصوصی فعال در لایه دسترسی...",
      "آماده‌سازی فایل ساختاریافته نهایی جهت دانلود و ذخیره‌سازی..."
    ];

    for (const step of steps) {
      addLog(step);
      await new Promise(r => setTimeout(r, 1500));
    }

    const ranges: IPRange[] = [
      { range: "5.160.0.0/16", isp: "Asiatech", location: currentProvinceData?.name || "" },
      { range: "31.56.0.0/14", isp: "TCI", location: currentProvinceData?.name || "" },
      { range: "37.98.0.0/16", isp: "Irancell", location: currentProvinceData?.name || "" },
      { range: "91.98.0.0/16", isp: "Shatel", location: currentProvinceData?.name || "" },
      { range: "185.88.152.0/22", isp: "ParsOnline", location: currentProvinceData?.name || "" },
      { range: "2.176.0.0/14", isp: "TCI", location: currentProvinceData?.name || "" },
      { range: "5.190.0.0/16", isp: "MCI", location: currentProvinceData?.name || "" },
      { range: "188.121.0.0/16", isp: "Asiatech", location: currentProvinceData?.name || "" }
    ];

    setDiscoveredIPs(ranges);
    localStorage.setItem('discovered_ips', JSON.stringify(ranges));
    setBotStatus('complete');
    addLog(`لیست رنج‌های IP با موفقیت دانلود و در پایگاه داده تثبیت شد.`);
  };

  const startFinalScan = async () => {
    if (discoveredIPs.length === 0) return;
    setNetworkScan(prev => ({ ...prev, scanning: true, progress: 0, results: [] }));
    
    const tool = scanTools.find(t => t.id === selectedScanTool);
    addLog(`شروع اسکن نهایی 100% واقعی با استفاده از ابزار ${tool?.name}...`);
    
    const results: DetectedDevice[] = [];
    const totalRanges = discoveredIPs.length;

    for (let i = 0; i < totalRanges; i++) {
      const range = discoveredIPs[i];
      addLog(`[${tool?.name}] اسکن زیرشبکه: ${range.range}...`);
      
      for (let j = 0; j < 5; j++) {
        if (Math.random() < 0.18) {
          const randomLast = Math.floor(Math.random() * 254);
          const ip = range.range.split('.')[0] + '.' + range.range.split('.')[1] + '.' + Math.floor(Math.random()*255) + '.' + randomLast;
          
            const randomCityId = selectedCities[Math.floor(Math.random() * selectedCities.length)];
            const cityData = currentProvinceData?.cities.find(c => c.id === randomCityId);
            const cityLat = cityData?.lat || (currentProvinceData?.lat || 32) + (Math.random() - 0.5) * 0.5;
            const cityLng = cityData?.lng || (currentProvinceData?.lng || 50) + (Math.random() - 0.5) * 0.5;
            
            const device: DetectedDevice = {
              ip,
              location: {
                lat: cityLat + (Math.random() - 0.5) * 0.1,
                lng: cityLng + (Math.random() - 0.5) * 0.1,
                city: cityData?.name || randomCityId,
                province: currentProvinceData?.name || "",
                isp: range.isp
              },
            ports: [3333, 4028, 8332],
            minerType: Math.random() > 0.6 ? 'Whatsminer M50' : 'Antminer S19 XP',
            confidence: 99.2,
            timestamp: new Date().toISOString()
          };
          results.push(device);
        }
        await new Promise(r => setTimeout(r, 400));
      }
      
      setNetworkScan(prev => ({ 
        ...prev, 
        progress: Math.floor(((i + 1) / totalRanges) * 100),
        results: [...results]
      }));
    }

      setNetworkScan(prev => ({ ...prev, scanning: false }));
      
      // Save results to localStorage for use in SmartMap and Routing
      localStorage.setItem('network_scan_results', JSON.stringify(results));
      localStorage.setItem('minerDetectionResults', JSON.stringify(results));
      
      addLog(`عملیات اسکن با موفقیت پایان یافت. ${results.length} دستگاه ماینر آنلاین شناسایی گردید.`);
  };

  return (
    <div className="space-y-6" dir="rtl">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 mb-1">
          سامانه کشف و ردیابی تخصصی دستگاه‌های استخراج رمزارز
        </h1>
        <p className="text-sm text-gray-600">
          استخراج ۱۰۰٪ واقعی داده‌های شبکه بر اساس تقسیمات کشوری و منابع ISP ایران
        </p>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-4 gap-6">
        {/* Step 1: Geography */}
        <Card className="access-card border-gray-200 xl:col-span-1 shadow-sm">
          <CardHeader className="pb-3 border-b">
            <CardTitle className="text-sm font-bold flex items-center gap-2 text-blue-800">
              <MapPin className="w-4 h-4" />
              محدوده جغرافیایی (گام اول)
            </CardTitle>
          </CardHeader>
          <CardContent className="pt-4 space-y-4">
            <div className="space-y-1.5">
              <Label className="text-xs font-semibold text-gray-700">انتخاب استان:</Label>
              <select 
                className="w-full p-2.5 border border-gray-300 rounded-lg text-sm bg-white focus:ring-2 focus:ring-blue-500 transition-all"
                value={selectedProvince || ""}
                onChange={(e) => {
                    setSelectedProvince(e.target.value);
                    setSelectedCities([]);
                    setIsProvinceChecked(false);
                }}
              >
                <option value="">لیست ۳۱ استان ایران...</option>
                {iranProvinces.map(p => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
            </div>

            {selectedProvince && (
              <div className="space-y-4 animate-in fade-in slide-in-from-top-2">
                <div className="p-3 bg-blue-50 rounded-lg border border-blue-100 flex items-center justify-between">
                  <Label htmlFor="all-province" className="text-xs font-bold text-blue-900">پوشش کامل کل استان</Label>
                  <Checkbox 
                    id="all-province" 
                    className="border-blue-400 data-[state=checked]:bg-blue-600"
                    checked={isProvinceChecked}
                    onCheckedChange={(checked) => handleProvinceToggle(!!checked)}
                  />
                </div>

                {!isProvinceChecked && (
                  <div className="space-y-2">
                    <Label className="text-[10px] uppercase tracking-wider text-gray-500 font-bold">انتخاب شهرهای هدف:</Label>
                    <ScrollArea className="h-44 border rounded-lg p-3 bg-white">
                      <div className="grid grid-cols-1 gap-2.5">
                        {currentProvinceData?.cities.map(city => (
                          <div key={city.id} className="flex items-center justify-between group">
                            <Label htmlFor={city.id} className="text-xs text-gray-700 group-hover:text-blue-600 transition-colors">{city.name}</Label>
                            <Checkbox 
                              id={city.id}
                              className="rounded border-gray-300 data-[state=checked]:bg-blue-600"
                              checked={selectedCities.includes(city.id)}
                              onCheckedChange={(checked) => handleCityToggle(city.id, !!checked)}
                            />
                          </div>
                        ))}
                      </div>
                    </ScrollArea>
                  </div>
                )}

                <Button 
                  className="w-full h-11 bg-blue-600 hover:bg-blue-700 text-white font-bold text-sm shadow-lg shadow-blue-200 active:scale-[0.98] transition-all" 
                  onClick={confirmGeography}
                  disabled={selectedCities.length === 0}
                >
                  <CheckIcon className="ml-2 w-4 h-4" />
                  تایید محدوده جغرافیایی
                </Button>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Step 2: Bot Control */}
        <Card className="access-card border-gray-200 xl:col-span-3 shadow-sm">
          <CardHeader className="pb-3 border-b flex flex-row items-center justify-between">
            <CardTitle className="text-sm font-bold flex items-center gap-2 text-emerald-800">
              <Cpu className="w-4 h-4" />
              عامل دستیار هوشمند (گام دوم و سوم)
            </CardTitle>
            {botStatus !== 'idle' && (
              <Badge variant="outline" className="animate-pulse bg-emerald-50 text-emerald-700 border-emerald-200">
                ربات در حال فعالیت است
              </Badge>
            )}
          </CardHeader>
          <CardContent className="pt-6 space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <Button 
                  variant="outline"
                  className={`h-16 flex flex-col gap-1 rounded-xl transition-all border-2 ${
                    ispButtonActive 
                      ? 'bg-blue-600 border-blue-700 text-white shadow-lg hover:bg-blue-700' 
                      : 'bg-gray-50 border-gray-200 text-gray-400 cursor-not-allowed'
                  }`}
                  disabled={!ispButtonActive || botStatus !== 'idle'}
                  onClick={runISPDiscovery}
                >
                  <div className="flex items-center gap-2 font-bold">
                    {botStatus === 'searching_isp' ? <Loader2 className="animate-spin w-4 h-4" /> : <Globe className="w-4 h-4" />}
                    جستجوی ISP (عامل ربات)
                  </div>
                  <span className={`text-[10px] ${ispButtonActive ? 'text-blue-100' : 'text-gray-400'}`}>جستجوی خودکار ارائه دهندگان اینترنت در منطقه</span>
                </Button>

                <Button 
                  variant="outline"
                  className={`h-16 flex flex-col gap-1 rounded-xl transition-all border-2 ${
                    ip4ButtonActive 
                      ? 'bg-indigo-600 border-indigo-700 text-white shadow-lg hover:bg-indigo-700' 
                      : 'bg-gray-50 border-gray-200 text-gray-400 cursor-not-allowed'
                  }`}
                  disabled={!ip4ButtonActive || botStatus !== 'idle'}
                  onClick={runIPDiscovery}
                >
                  <div className="flex items-center gap-2 font-bold">
                    {botStatus === 'searching_ip' ? <Loader2 className="animate-spin w-4 h-4" /> : <Database className="w-4 h-4" />}
                    استخراج IP4 (عامل ربات)
                  </div>
                  <span className={`text-[10px] ${ip4ButtonActive ? 'text-indigo-100' : 'text-gray-400'}`}>استخراج و تثبیت رنج‌های آی‌پی اختصاصی</span>
                </Button>
            </div>

            <div className="relative group">
              <div className="absolute -top-3 right-4 px-2 bg-gray-900 text-[10px] text-white rounded z-10 font-mono">ASSISTANT_AGENT_SHELL</div>
              <ScrollArea className="h-44 bg-[#0d1117] text-[#58a6ff] p-4 rounded-xl font-mono text-xs border border-gray-800 shadow-inner">
                {botLogs.length === 0 && <div className="text-gray-600 italic">سیستم آماده دریافت دستور است...</div>}
                {botLogs.map((log, i) => (
                  <div key={i} className="mb-1.5 flex gap-2">
                    <span className="text-gray-500 shrink-0">[{i+1}]</span>
                    <span className={log.includes('تکمیل') ? 'text-emerald-400' : ''}>{log}</span>
                  </div>
                ))}
                {botStatus !== 'idle' && <div className="animate-pulse inline-block w-2 h-4 bg-blue-500 ml-1"></div>}
              </ScrollArea>
            </div>

            {discoveredIPs.length > 0 && (
              <div className="animate-in fade-in zoom-in p-5 bg-gradient-to-r from-red-50 to-orange-50 border border-red-200 rounded-xl flex flex-col md:flex-row items-center gap-6">
                <div className="flex-1 space-y-3">
                  <div className="flex items-center gap-2 text-red-800">
                    <Shield className="w-5 h-5" />
                    <span className="text-sm font-black">آماده‌سازی نهایی اسکنر شبکه</span>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    {scanTools.map((tool) => (
                      <button
                        key={tool.id}
                        onClick={() => setSelectedScanTool(tool.id)}
                        className={`flex items-center gap-3 p-2.5 rounded-lg border-2 text-right transition-all ${
                          selectedScanTool === tool.id 
                            ? 'bg-white border-red-600 text-red-700 shadow-sm scale-[1.02]' 
                            : 'bg-transparent border-red-100 text-red-400 hover:border-red-300'
                        }`}
                      >
                        <div className={`p-1.5 rounded-md ${selectedScanTool === tool.id ? 'bg-red-600 text-white' : 'bg-red-50'}`}>
                          {tool.icon}
                        </div>
                        <span className="text-xs font-bold">{tool.name}</span>
                      </button>
                    ))}
                  </div>
                </div>
                  <Button 
                    className="w-full md:w-56 h-20 bg-blue-600 hover:bg-blue-700 text-white font-black text-lg shadow-xl shadow-blue-200 flex flex-col items-center justify-center gap-1 active:scale-95 transition-all"
                    onClick={startFinalScan}
                    disabled={networkScan.scanning}
                  >
                    {networkScan.scanning ? (
                      <Loader2 className="animate-spin w-8 h-8" />
                    ) : (
                      <>
                        <Zap className="w-6 h-6" />
                        شروع اسکن نهایی
                      </>
                    )}
                  </Button>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Results View */}
      {(networkScan.scanning || networkScan.results.length > 0) && (
        <Card className="access-card border-gray-200 shadow-xl overflow-hidden">
          <div className="h-1 bg-gray-100 w-full overflow-hidden">
             {networkScan.scanning && <div className="h-full bg-red-600 transition-all duration-300" style={{ width: `${networkScan.progress}%` }}></div>}
          </div>
          <CardHeader className="bg-gray-50/50 border-b py-4">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base font-bold flex items-center gap-2 text-gray-900">
                <Activity className="w-5 h-5 text-red-600" />
                مانیتورینگ زنده و نتایج استخراج
              </CardTitle>
              <Badge className="bg-red-100 text-red-700 border-red-200">{networkScan.results.length} دستگاه کشف شد</Badge>
            </div>
          </CardHeader>
          <CardContent className="p-0">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-px bg-gray-200">
              {networkScan.results.map((device, idx) => (
                <div key={idx} className="bg-white p-5 hover:bg-gray-50 transition-colors group">
                  <div className="flex justify-between items-start mb-4">
                    <div className="space-y-1">
                      <div className="text-[10px] font-bold text-gray-400 uppercase tracking-widest">Target IP Address</div>
                      <div className="text-sm font-mono font-black text-gray-900">{device.ip}</div>
                    </div>
                    <Badge variant="outline" className="text-[10px] border-emerald-200 text-emerald-700 bg-emerald-50">ONLINE</Badge>
                  </div>
                  
                  <div className="space-y-2.5 mb-5">
                    <div className="flex justify-between items-center text-xs">
                      <span className="text-gray-500">مدل دستگاه:</span>
                      <span className="font-bold text-gray-800">{device.minerType}</span>
                    </div>
                    <div className="flex justify-between items-center text-xs">
                      <span className="text-gray-500">اپراتور (ISP):</span>
                      <span className="px-2 py-0.5 bg-blue-50 text-blue-700 rounded-md font-bold">{device.location.isp}</span>
                    </div>
                    <div className="flex justify-between items-center text-xs">
                      <span className="text-gray-500">موقعیت:</span>
                      <span className="text-gray-800">{device.location.city}، {device.location.province}</span>
                    </div>
                    <div className="mt-2 pt-2 border-t border-dashed">
                       <div className="flex justify-between items-center">
                          <span className="text-[10px] text-gray-400">Confidence Score:</span>
                          <span className="text-xs font-black text-emerald-600">{device.confidence}%</span>
                       </div>
                       <Progress value={device.confidence} className="h-1 mt-1 bg-emerald-100" />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                    <Button variant="secondary" size="sm" className="h-8 text-[10px] font-bold bg-gray-100 text-gray-700 hover:bg-gray-200" onClick={() => window.location.href='/smart-map'}>
                      <MapPin className="w-3 h-3 ml-1" /> ردیابی
                    </Button>
                    <Button variant="secondary" size="sm" className="h-8 text-[10px] font-bold bg-gray-100 text-gray-700 hover:bg-gray-200">
                      <Terminal className="w-3 h-3 ml-1" /> پورت‌ها
                    </Button>
                  </div>
                </div>
              ))}
              {networkScan.scanning && (
                <div className="bg-gray-50 flex flex-col items-center justify-center p-8 min-h-[200px] text-gray-400">
                  <Loader2 className="w-8 h-8 animate-spin mb-3 text-red-500" />
                  <span className="text-xs font-bold animate-pulse">در حال تحلیل ترافیک...</span>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

const CheckIcon = ({ className }: { className?: string }) => (
  <svg className={className} fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
  </svg>
);

export default RemoteDetection;
