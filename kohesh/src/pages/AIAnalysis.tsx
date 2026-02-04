
import React, { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Navigation, Activity } from 'lucide-react';

interface AIModel {
  id: string;
  name: string;
  type: string;
  accuracy: number;
  status: 'active' | 'loading' | 'inactive';
  description: string;
}

const AIAnalysis = () => {
  const [selectedModel, setSelectedModel] = useState('tensorflow-js');
  const [analysisProgress, setAnalysisProgress] = useState(0);
  const [isAnalyzing, setIsAnalyzing] = useState(false);

  const aiModels: AIModel[] = [
    {
      id: 'tensorflow-js',
      name: 'TensorFlow.js',
      type: 'شبکه عصبی',
      accuracy: 94.2,
      status: 'active',
      description: 'مدل یادگیری عمیق برای تشخیص الگوهای ترافیک شبکه'
    },
    {
      id: 'huggingface-transformers',
      name: 'Hugging Face Transformers',
      type: 'ترنسفورمر',
      accuracy: 91.8,
      status: 'active',
      description: 'تحلیل متن و الگوریتم‌های NLP برای تشخیص API ماینرها'
    },
    {
      id: 'onnx-runtime',
      name: 'ONNX Runtime',
      type: 'مدل آماده',
      accuracy: 88.5,
      status: 'loading',
      description: 'مدل پیش‌آموزش دیده برای تشخیص دستگاه‌های سخت‌افزاری'
    },
    {
      id: 'mediapipe',
      name: 'MediaPipe',
      type: 'بینایی ماشین',
      accuracy: 89.7,
      status: 'active',
      description: 'تحلیل تصاویر حرارتی و تشخیص نقاط داغ'
    },
    {
      id: 'brain-js',
      name: 'Brain.js',
      type: 'شبکه عصبی ساده',
      accuracy: 82.3,
      status: 'active',
      description: 'تحلیل سریع الگوهای RF و مغناطیسی'
    }
  ];

  const startAnalysis = () => {
    setIsAnalyzing(true);
    setAnalysisProgress(0);
    
    const interval = setInterval(() => {
      setAnalysisProgress(prev => {
        if (prev >= 100) {
          clearInterval(interval);
          setIsAnalyzing(false);
          return 100;
        }
        return prev + 2;
      });
    }, 100);
  };

  const activeModel = aiModels.find(model => model.id === selectedModel);

  return (
    <div className="space-y-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground mb-2">
          هوش مصنوعی و یادگیری ماشین
        </h1>
        <p className="text-muted-foreground">
          استفاده از مدل‌های هوش مصنوعی برای تحلیل دقیق‌تر داده‌ها و شناسایی الگوهای پیچیده دستگاه‌های ماینر
        </p>
      </div>

      {/* Model Selection */}
      <Card className="bg-card/50 backdrop-blur-sm border-border">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Navigation className="w-5 h-5 text-primary" />
            انتخاب مدل هوش مصنوعی
          </CardTitle>
          <CardDescription>
            مدل مناسب را برای نوع تحلیل مورد نظر انتخاب کنید
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">مدل فعال</label>
              <Select value={selectedModel} onValueChange={setSelectedModel}>
                <SelectTrigger>
                  <SelectValue placeholder="انتخاب مدل..." />
                </SelectTrigger>
                <SelectContent>
                  {aiModels.map((model) => (
                    <SelectItem key={model.id} value={model.id}>
                      {model.name} - {model.type}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            
            <div className="flex items-end">
              <Button 
                onClick={startAnalysis}
                disabled={isAnalyzing}
                className="bg-primary hover:bg-primary/90 w-full"
              >
                {isAnalyzing ? 'در حال تحلیل...' : 'شروع تحلیل هوشمند'}
              </Button>
            </div>
          </div>

          {activeModel && (
            <div className="p-4 bg-secondary/30 rounded-lg">
              <div className="flex items-center justify-between mb-2">
                <h4 className="font-medium text-foreground">{activeModel.name}</h4>
                <Badge 
                  variant={activeModel.status === 'active' ? 'default' : 'secondary'}
                  className={activeModel.status === 'active' ? 'bg-accent text-accent-foreground' : ''}
                >
                  {activeModel.status === 'active' ? 'فعال' :
                   activeModel.status === 'loading' ? 'در حال بارگذاری' : 'غیرفعال'}
                </Badge>
              </div>
              <p className="text-sm text-muted-foreground mb-2">{activeModel.description}</p>
              <div className="flex items-center gap-2">
                <span className="text-sm text-muted-foreground">دقت:</span>
                <span className="text-sm font-medium text-primary">{activeModel.accuracy}%</span>
              </div>
            </div>
          )}

          {isAnalyzing && (
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span>پیشرفت تحلیل</span>
                <span>{analysisProgress}%</span>
              </div>
              <Progress value={analysisProgress} />
            </div>
          )}
        </CardContent>
      </Card>

      <Tabs defaultValue="models" className="space-y-6">
        <TabsList className="grid w-full grid-cols-3 bg-secondary">
          <TabsTrigger value="models">مدل‌های موجود</TabsTrigger>
          <TabsTrigger value="analysis">نتایج تحلیل</TabsTrigger>
          <TabsTrigger value="training">آموزش مدل</TabsTrigger>
        </TabsList>

        {/* Available Models Tab */}
        <TabsContent value="models" className="space-y-4">
          <div className="grid gap-4">
            {aiModels.map((model) => (
              <Card key={model.id} className="bg-card/50 backdrop-blur-sm border-border">
                <CardContent className="p-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-4">
                      <div className={`w-3 h-3 rounded-full ${
                        model.status === 'active' ? 'bg-accent detection-pulse' :
                        model.status === 'loading' ? 'bg-primary' : 'bg-muted-foreground'
                      }`}></div>
                      
                      <div>
                        <h4 className="font-medium text-foreground">{model.name}</h4>
                        <p className="text-sm text-muted-foreground">{model.description}</p>
                        <div className="flex items-center gap-4 mt-1">
                          <span className="text-xs text-primary font-medium">نوع: {model.type}</span>
                          <span className="text-xs text-accent font-medium">دقت: {model.accuracy}%</span>
                        </div>
                      </div>
                    </div>
                    
                    <div className="flex items-center gap-2">
                      <Badge 
                        variant={model.status === 'active' ? 'default' : 'secondary'}
                        className={model.status === 'active' ? 'bg-accent text-accent-foreground' : ''}
                      >
                        {model.status === 'active' ? 'فعال' :
                         model.status === 'loading' ? 'بارگذاری' : 'غیرفعال'}
                      </Badge>
                      
                      <Button 
                        size="sm" 
                        variant="outline"
                        disabled={model.status === 'loading'}
                      >
                        {model.status === 'active' ? 'استفاده' : 'فعال‌سازی'}
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        {/* Analysis Results Tab */}
        <TabsContent value="analysis" className="space-y-6">
          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="w-5 h-5 text-accent" />
                نتایج تحلیل هوشمند
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
                <div className="text-center p-4 bg-primary/10 rounded-lg">
                  <div className="text-2xl font-bold text-primary">96.4%</div>
                  <div className="text-sm text-muted-foreground">دقت کل</div>
                </div>
                <div className="text-center p-4 bg-accent/10 rounded-lg">
                  <div className="text-2xl font-bold text-accent">15</div>
                  <div className="text-sm text-muted-foreground">الگوی شناخته شده</div>
                </div>
                <div className="text-center p-4 bg-destructive/10 rounded-lg">
                  <div className="text-2xl font-bold text-destructive">3</div>
                  <div className="text-sm text-muted-foreground">مورد مشکوک</div>
                </div>
                <div className="text-center p-4 bg-secondary/30 rounded-lg">
                  <div className="text-2xl font-bold text-foreground">1.8s</div>
                  <div className="text-sm text-muted-foreground">زمان پردازش</div>
                </div>
              </div>

              <div className="space-y-4">
                <h4 className="font-medium">تحلیل‌های اخیر:</h4>
                <div className="space-y-3">
                  <div className="flex items-center justify-between p-3 bg-accent/10 rounded-lg border border-accent/20">
                    <div>
                      <div className="font-medium text-accent">شناسایی ASIC Antminer</div>
                      <div className="text-sm text-muted-foreground">مدل: TensorFlow.js • دقت: 96.4%</div>
                    </div>
                    <Badge variant="outline" className="border-accent text-accent">
                      تایید شده
                    </Badge>
                  </div>
                  
                  <div className="flex items-center justify-between p-3 bg-primary/10 rounded-lg border border-primary/20">
                    <div>
                      <div className="font-medium text-primary">الگوی ترافیک ماینینگ</div>
                      <div className="text-sm text-muted-foreground">مدل: Hugging Face • دقت: 91.8%</div>
                    </div>
                    <Badge variant="outline" className="border-primary text-primary">
                      احتمال بالا
                    </Badge>
                  </div>
                  
                  <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                    <div>
                      <div className="font-medium">تحلیل حرارتی</div>
                      <div className="text-sm text-muted-foreground">مدل: MediaPipe • دقت: 89.7%</div>
                    </div>
                    <Badge variant="secondary">
                      در حال بررسی
                    </Badge>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Model Training Tab */}
        <TabsContent value="training" className="space-y-6">
          <Card className="bg-card/50 backdrop-blur-sm border-border">
            <CardHeader>
              <CardTitle>آموزش و بهینه‌سازی مدل</CardTitle>
              <CardDescription>
                بهبود دقت مدل‌ها با استفاده از داده‌های جمع‌آوری شده
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="text-center p-4 bg-primary/10 rounded-lg">
                  <div className="text-xl font-bold text-primary">1,247</div>
                  <div className="text-sm text-muted-foreground">نمونه آموزشی</div>
                </div>
                <div className="text-center p-4 bg-accent/10 rounded-lg">
                  <div className="text-xl font-bold text-accent">89.2%</div>
                  <div className="text-sm text-muted-foreground">دقت اولیه</div>
                </div>
                <div className="text-center p-4 bg-secondary/30 rounded-lg">
                  <div className="text-xl font-bold text-foreground">45 min</div>
                  <div className="text-sm text-muted-foreground">زمان آموزش</div>
                </div>
              </div>

              <div className="space-y-3">
                <Button className="w-full bg-primary hover:bg-primary/90" disabled>
                  شروع آموزش مجدد مدل
                </Button>
                <p className="text-sm text-muted-foreground text-center">
                  آموزش مدل با داده‌های جدید جمع‌آوری شده از اسکن‌های اخیر
                </p>
              </div>

              <div className="p-4 bg-muted/20 rounded-lg">
                <p className="text-sm text-muted-foreground">
                  <strong>نکته:</strong> آموزش مدل‌های هوش مصنوعی نیاز به قدرت پردازشی بالا دارد.
                  برای بهترین نتایج، از GPU یا TPU استفاده کنید.
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default AIAnalysis;
