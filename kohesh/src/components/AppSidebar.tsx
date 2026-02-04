import React from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarTrigger,
  useSidebar,
} from "@/components/ui/sidebar";
import { 
  MapPin, Navigation, Map, Network, CreditCard, User, Home, Radar, 
  Brain, Database, Cpu, Route, BarChart, FileText, Settings, 
  TestTube, LogOut, LogIn
} from 'lucide-react';
import { useAuth } from '@/hooks/useAuth';
import { Button } from './ui/button';

const menuItems = [
  { title: "داشبورد اصلی", url: "/", icon: Home, category: "main" },
  { title: "تشخیص از راه دور", url: "/remote-detection", icon: Radar, category: "detection" },
  { title: "تشخیص محلی", url: "/local-detection", icon: MapPin, category: "detection" },
  { title: "نقشه هوشمند", url: "/smart-map", icon: Map, category: "mapping" },
  { title: "مسیریابی", url: "/routing", icon: Route, category: "mapping" },
  { title: "هوش مصنوعی", url: "/ai-analysis", icon: Brain, category: "ai" },
  { title: "پایگاه داده", url: "/database", icon: Database, category: "data" },
  { title: "گزارشات", url: "/reports", icon: FileText, category: "data" },
  { title: "آمارها", url: "/statistics", icon: BarChart, category: "data" },
  { title: "تنظیمات سخت‌افزار", url: "/hardware", icon: Cpu, category: "hardware" },
  { title: "کالیبراسیون", url: "/calibration", icon: Settings, category: "hardware" },
  { title: "تست تجهیزات", url: "/equipment-test", icon: TestTube, category: "hardware" },
  { title: "اشتراک و پرداخت", url: "/pricing", icon: CreditCard, category: "billing" },
  { title: "حساب کاربری", url: "/account", icon: User, category: "billing" }
];

const categories: Record<string, string> = {
  main: "داشبورد",
  detection: "تشخیص و شناسایی",
  mapping: "نقشه‌سازی و مسیریابی",
  ai: "هوش مصنوعی",
  data: "مدیریت داده",
  hardware: "سخت‌افزار و تنظیمات",
  billing: "اشتراک و پرداخت"
};

export function AppSidebar() {
  const { state } = useSidebar();
  const location = useLocation();
  const navigate = useNavigate();
  const { user, signOut } = useAuth();
  const currentPath = location.pathname;
  const collapsed = state === "collapsed";

  const isActive = (path: string) => currentPath === path;

  const groupedItems = Object.entries(categories).map(([key, label]) => ({
    label,
    key,
    items: menuItems.filter(item => item.category === key),
    hasActive: menuItems.filter(item => item.category === key).some(item => isActive(item.url))
  }));

  const handleLogout = async () => {
    await signOut();
    navigate('/auth');
  };

  return (
    <Sidebar
      className={`${collapsed ? "w-16" : "w-64"} border-r-2 border-[#8b4513]`}
      style={{ background: 'linear-gradient(135deg, #deb887 0%, #cd853f 100%)' }}
      collapsible="icon"
    >
      <SidebarTrigger className="m-3 access-button" />

      <SidebarContent className="px-3">
        {groupedItems.map((group) => (
          <SidebarGroup key={group.key} className="mb-2">
            <SidebarGroupLabel 
              className="text-black px-2 py-2 font-bold text-sm"
              style={{ fontFamily: 'BNazanin', fontWeight: '700' }}
            >
              {!collapsed && group.label}
            </SidebarGroupLabel>

            <SidebarGroupContent>
              <SidebarMenu>
                {group.items.map((item) => (
                  <SidebarMenuItem key={item.title}>
                    <SidebarMenuButton asChild>
                      <NavLink 
                        to={item.url} 
                        end 
                        className={({ isActive: linkActive }) => 
                          `sidebar-button ${linkActive ? 'active' : ''}`
                        }
                      >
                        <item.icon className="w-5 h-5 flex-shrink-0" />
                        {!collapsed && (
                          <span className="text-sm font-bold overflow-visible whitespace-normal leading-tight" style={{ fontFamily: 'BNazanin' }}>
                            {item.title}
                          </span>
                        )}
                      </NavLink>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                ))}
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        ))}

        <div className="mt-auto pt-4 border-t border-[#8b4513]/30">
          {user ? (
            <div className="space-y-2 px-2">
              {!collapsed && (
                <div className="text-xs text-gray-700 truncate px-2" style={{ fontFamily: 'BNazanin' }}>
                  {user.email}
                </div>
              )}
              <Button 
                variant="ghost" 
                className="w-full justify-start text-red-700 hover:bg-red-100 hover:text-red-800"
                onClick={handleLogout}
              >
                <LogOut className="w-5 h-5 ml-2" />
                {!collapsed && <span style={{ fontFamily: 'BNazanin' }}>خروج</span>}
              </Button>
            </div>
          ) : (
            <Button 
              variant="ghost" 
              className="w-full justify-start text-green-700 hover:bg-green-100 hover:text-green-800"
              onClick={() => navigate('/auth')}
            >
              <LogIn className="w-5 h-5 ml-2" />
              {!collapsed && <span style={{ fontFamily: 'BNazanin' }}>ورود / ثبت‌نام</span>}
            </Button>
          )}
        </div>
      </SidebarContent>
    </Sidebar>
  );
}
