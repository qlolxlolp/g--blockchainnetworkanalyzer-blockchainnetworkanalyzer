
import React from 'react';
import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar";
import { AppSidebar } from "@/components/AppSidebar";
import { Navigation } from "@/components/Navigation";
import { Search, Settings } from 'lucide-react';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({
  children
}) => {
  return <SidebarProvider>
      <div className="min-h-screen flex w-full" style={{ background: 'linear-gradient(135deg, #f5f5dc 0%, #deb887 100%)' }}>
        <AppSidebar />
        
        <div className="flex-1 flex flex-col">
          {/* Header */}
          <header className="access-toolbar flex items-center justify-between" style={{ background: 'linear-gradient(to bottom, #deb887 0%, #cd853f 100%)' }}>
            <div className="flex items-center gap-4">
              <SidebarTrigger className="access-button" />
              <div className="flex items-center gap-4">
                <div className="w-8 h-8 bg-[#8b4513] flex items-center justify-center rounded border-2 border-black">
                  <Search className="w-5 h-5 text-white" />
                </div>
                <span className="text-lg font-bold text-black" style={{ fontFamily: 'Amiri Quran' }}>
                  شـــبـحِ حَشــبی - سامانه شناسایی پیشرفته
                </span>
              </div>
            </div>
            
            <Navigation />
            
            <div className="flex items-center gap-4">
              <Settings className="w-5 h-5 text-black" />
            </div>
          </header>

          {/* Main Content */}
          <main className="flex-1 p-4" style={{ background: 'linear-gradient(135deg, #f5f5dc 0%, #deb887 100%)' }}>
            <div className="access-card min-h-full">
              {children}
            </div>
          </main>

          {/* Footer */}
          <footer className="access-status-bar">
            <div className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="text-black font-bold" style={{ fontFamily: 'Amiri Quran' }}>آماده</span>
                <span className="text-black font-bold" style={{ fontFamily: 'Amiri Quran' }}>سامانه فعال</span>
              </div>
              <div className="company-info text-center border-t-2 border-black pt-2">
                <div className="mb-2 text-black font-bold" style={{ fontFamily: 'Amiri Quran' }}>
                  © کلیه حقوق محفوظ است - طراحی، توسعه و اجرا: عرفان رجبی
                </div>
                <div className="flex flex-col lg:flex-row justify-center items-center gap-3 text-sm">
                  <span className="text-black font-bold" style={{ fontFamily: 'Amiri Quran' }}>شرکت تلاشگر ایلام (سهامی خاص)</span>
                  <span className="text-black font-bold" style={{ fontFamily: 'Amiri Quran' }}>|</span>
                  <span className="text-black font-bold" style={{ fontFamily: 'Amiri Quran' }}>اسفند 1402، بهار 1404</span>
                  <span className="text-black font-bold" style={{ fontFamily: 'Amiri Quran' }}>|</span>
                  <span dir="ltr" className="text-black font-bold" style={{ fontFamily: 'Amiri Quran' }}>Erfanrajabee@gmail.com</span>
                </div>
                <div className="text-sm mt-2 text-black font-bold" style={{ fontFamily: 'Amiri Quran' }}>
                  آدرس دفتر: ایلام - خیابان سعدی شمالی - مجتمع اداری تجاری رجبی - طبقه آخر
                </div>
              </div>
            </div>
          </footer>
        </div>
      </div>
    </SidebarProvider>;
};

export default Layout;
