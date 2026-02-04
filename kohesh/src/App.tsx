import { Toaster } from "@/components/ui/toaster";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider } from "./hooks/useAuth";
import Layout from "./components/Layout";
import Index from "./pages/Index";
import Auth from "./pages/Auth";
import RemoteDetection from "./pages/RemoteDetection";
import LocalDetection from "./pages/LocalDetection";
import SmartMap from "./pages/SmartMap";
import AIAnalysis from "./pages/AIAnalysis";
import Database from "./pages/Database";
import Hardware from "./pages/Hardware";
import Routing from "./pages/Routing";
import LocationTracking from "./pages/LocationTracking";
import MachineLearning from "./pages/MachineLearning";
import PatternRecognition from "./pages/PatternRecognition";
import Reports from "./pages/Reports";
import Statistics from "./pages/Statistics";
import Calibration from "./pages/Calibration";
import EquipmentTest from "./pages/EquipmentTest";
import Pricing from "./pages/Pricing";
import SubscriptionSuccess from "./pages/SubscriptionSuccess";
import Account from "./pages/Account";
import NotFound from "./pages/NotFound";

const queryClient = new QueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <AuthProvider>
      <TooltipProvider>
        <Toaster />
        <Sonner />
        <BrowserRouter>
          <Routes>
            <Route path="/auth" element={<Auth />} />
            <Route path="/" element={<Layout><Index /></Layout>} />
            <Route path="/remote-detection" element={<Layout><RemoteDetection /></Layout>} />
            <Route path="/local-detection" element={<Layout><LocalDetection /></Layout>} />
            <Route path="/smart-map" element={<Layout><SmartMap /></Layout>} />
            <Route path="/ai-analysis" element={<Layout><AIAnalysis /></Layout>} />
            <Route path="/database" element={<Layout><Database /></Layout>} />
            <Route path="/hardware" element={<Layout><Hardware /></Layout>} />
            <Route path="/routing" element={<Layout><Routing /></Layout>} />
            <Route path="/location-tracking" element={<Layout><LocationTracking /></Layout>} />
            <Route path="/machine-learning" element={<Layout><MachineLearning /></Layout>} />
            <Route path="/pattern-recognition" element={<Layout><PatternRecognition /></Layout>} />
            <Route path="/reports" element={<Layout><Reports /></Layout>} />
            <Route path="/statistics" element={<Layout><Statistics /></Layout>} />
            <Route path="/calibration" element={<Layout><Calibration /></Layout>} />
            <Route path="/equipment-test" element={<Layout><EquipmentTest /></Layout>} />
            <Route path="/pricing" element={<Layout><Pricing /></Layout>} />
            <Route path="/subscription/success" element={<Layout><SubscriptionSuccess /></Layout>} />
            <Route path="/account" element={<Layout><Account /></Layout>} />
            <Route path="*" element={<NotFound />} />
          </Routes>
        </BrowserRouter>
      </TooltipProvider>
    </AuthProvider>
  </QueryClientProvider>
);

export default App;
