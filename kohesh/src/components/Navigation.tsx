
import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { ChevronDown } from 'lucide-react';

const navigationCategories = {
  detection: {
    title: "تشخیص و شناسایی",
    items: [
      { title: "تشخیص از راه دور", path: "/remote-detection" },
      { title: "تشخیص محلی", path: "/local-detection" }
    ]
  },
  mapping: {
    title: "نقشه‌سازی و مسیریابی",
    items: [
      { title: "نقشه هوشمند", path: "/smart-map" },
      { title: "مسیریابی", path: "/routing" },
      { title: "مکان‌یابی", path: "/location-tracking" }
    ]
  },
  ai: {
    title: "هوش مصنوعی",
    items: [
      { title: "تحلیل AI", path: "/ai-analysis" },
      { title: "یادگیری ماشین", path: "/machine-learning" },
      { title: "تشخیص الگو", path: "/pattern-recognition" }
    ]
  },
  data: {
    title: "مدیریت داده",
    items: [
      { title: "دیتابیس", path: "/database" },
      { title: "گزارشات", path: "/reports" },
      { title: "آمارها", path: "/statistics" }
    ]
  },
  hardware: {
    title: "سخت‌افزار",
    items: [
      { title: "تنظیمات سخت‌افزار", path: "/hardware" },
      { title: "کالیبراسیون", path: "/calibration" },
      { title: "تست تجهیزات", path: "/equipment-test" }
    ]
  }
};

export function Navigation() {
  return (
    <nav className="flex items-center gap-6" style={{ fontFamily: 'BNazanin' }}>
      <NavLink 
        to="/" 
        className={({ isActive }) => 
          `text-sm px-3 py-2 rounded-md transition-colors ${
            isActive ? 'bg-primary/20 text-primary' : 'text-muted-foreground hover:text-foreground'
          }`
        }
      >
        داشبورد
      </NavLink>

      {Object.entries(navigationCategories).map(([key, category]) => (
        <DropdownMenu key={key}>
          <DropdownMenuTrigger className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground px-3 py-2 rounded-md transition-colors">
            {category.title}
            <ChevronDown className="w-3 h-3" />
          </DropdownMenuTrigger>
          <DropdownMenuContent className="bg-card border border-border shadow-lg" align="center">
            {category.items.map((item, index) => (
              <DropdownMenuItem key={index} className="p-0">
                <NavLink 
                  to={item.path}
                  className={({ isActive }) => 
                    `w-full px-3 py-2 text-sm transition-colors ${
                      isActive ? 'bg-primary/20 text-primary' : 'text-muted-foreground hover:text-foreground'
                    }`
                  }
                >
                  {item.title}
                </NavLink>
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      ))}
    </nav>
  );
}
