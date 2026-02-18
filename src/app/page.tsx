'use client';

import React, { useState, useEffect } from 'react';
import { Activity, TrendingUp, Network, AlertCircle, Zap, Shield } from 'lucide-react';
import { LineChart, Line, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';

// Sample data for demonstration
const networkData = [
  { time: '00:00', nodes: 1200, latency: 45, bandwidth: 80 },
  { time: '04:00', nodes: 1500, latency: 52, bandwidth: 85 },
  { time: '08:00', nodes: 2100, latency: 48, bandwidth: 90 },
  { time: '12:00', nodes: 2800, latency: 55, bandwidth: 88 },
  { time: '16:00', nodes: 2400, latency: 50, bandwidth: 92 },
  { time: '20:00', nodes: 2200, latency: 48, bandwidth: 87 },
  { time: '24:00', nodes: 1800, latency: 46, bandwidth: 85 },
];

const transactionData = [
  { name: 'Bitcoin', value: 35, color: '#3b82f6' },
  { name: 'Ethereum', value: 28, color: '#8b5cf6' },
  { name: 'Others', value: 37, color: '#ec4899' },
];

const minerData = [
  { pool: 'Mining Pool A', hashrate: 450 },
  { pool: 'Mining Pool B', hashrate: 320 },
  { pool: 'Mining Pool C', hashrate: 280 },
  { pool: 'Mining Pool D', hashrate: 180 },
  { pool: 'Others', hashrate: 170 },
];

export default function Home() {
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) return null;

  return (
    <div className="min-h-screen bg-black text-white">
      {/* Header */}
      <header className="border-b border-gray-800 bg-gray-900/50 backdrop-blur-sm sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-lg flex items-center justify-center">
              <Network className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-xl font-bold">Blockchain Network Analyzer</h1>
              <p className="text-xs text-gray-400">Real-time Network Intelligence</p>
            </div>
          </div>
          <div className="flex gap-4">
            <button className="px-4 py-2 rounded-lg bg-gray-800 hover:bg-gray-700 transition-colors text-sm font-medium">
              Settings
            </button>
            <button className="px-4 py-2 rounded-lg bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 transition-all text-sm font-medium">
              Connect Wallet
            </button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-6 py-8">
        {/* KPI Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          <KPICard
            icon={<Activity className="w-5 h-5" />}
            title="Active Nodes"
            value="2,847"
            change="+12.5%"
            color="blue"
          />
          <KPICard
            icon={<TrendingUp className="w-5 h-5" />}
            title="Avg Latency"
            value="48ms"
            change="-2.3%"
            color="green"
          />
          <KPICard
            icon={<Zap className="w-5 h-5" />}
            title="Network Throughput"
            value="89%"
            change="+5.1%"
            color="purple"
          />
          <KPICard
            icon={<Shield className="w-5 h-5" />}
            title="Security Score"
            value="94/100"
            change="+1.2%"
            color="pink"
          />
        </div>

        {/* Charts Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">
          {/* Network Activity Chart */}
          <div className="lg:col-span-2 bg-gray-900 border border-gray-800 rounded-xl p-6">
            <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
              <Activity className="w-5 h-5 text-blue-500" />
              Network Activity
            </h2>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={networkData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#333" />
                <XAxis dataKey="time" stroke="#666" />
                <YAxis stroke="#666" />
                <Tooltip 
                  contentStyle={{ backgroundColor: '#1a1a1a', border: '1px solid #333' }}
                  cursor={{ stroke: '#666' }}
                />
                <Legend />
                <Line type="monotone" dataKey="nodes" stroke="#3b82f6" dot={false} />
                <Line type="monotone" dataKey="latency" stroke="#ef4444" dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>

          {/* Blockchain Distribution */}
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
            <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
              <Network className="w-5 h-5 text-purple-500" />
              Network Share
            </h2>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={transactionData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, value }) => `${name} ${value}%`}
                  outerRadius={100}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {transactionData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip 
                  contentStyle={{ backgroundColor: '#1a1a1a', border: '1px solid #333' }}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Miner Pools and Alerts */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Mining Pools */}
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
            <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
              <Zap className="w-5 h-5 text-yellow-500" />
              Top Mining Pools
            </h2>
            <ResponsiveContainer width="100%" height={250}>
              <BarChart data={minerData} layout="vertical">
                <CartesianGrid strokeDasharray="3 3" stroke="#333" />
                <XAxis type="number" stroke="#666" />
                <YAxis dataKey="pool" type="category" width={100} stroke="#666" tick={{ fontSize: 12 }} />
                <Tooltip 
                  contentStyle={{ backgroundColor: '#1a1a1a', border: '1px solid #333' }}
                />
                <Bar dataKey="hashrate" fill="#f59e0b" />
              </BarChart>
            </ResponsiveContainer>
          </div>

          {/* Alerts and Status */}
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
            <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
              <AlertCircle className="w-5 h-5 text-red-500" />
              System Alerts
            </h2>
            <div className="space-y-3">
              <Alert type="warning" title="High Network Congestion" message="Mining difficulty has increased by 8% in the last hour" />
              <Alert type="info" title="New Block Detected" message="Block #847531 mined by Mining Pool A (2.4s ago)" />
              <Alert type="success" title="Network Healthy" message="All nodes reporting normal conditions" />
              <Alert type="info" title="Transaction Spike" message="Processing 2,847 transactions per second" />
            </div>
          </div>
        </div>

        {/* Footer Stats */}
        <div className="mt-8 grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
          <div className="bg-gray-900 border border-gray-800 rounded-lg p-4">
            <p className="text-gray-400 mb-1">Total Hash Rate</p>
            <p className="text-2xl font-bold">1.42 EH/s</p>
          </div>
          <div className="bg-gray-900 border border-gray-800 rounded-lg p-4">
            <p className="text-gray-400 mb-1">Avg Block Time</p>
            <p className="text-2xl font-bold">9.8s</p>
          </div>
          <div className="bg-gray-900 border border-gray-800 rounded-lg p-4">
            <p className="text-gray-400 mb-1">Network Uptime</p>
            <p className="text-2xl font-bold">99.97%</p>
          </div>
        </div>
      </main>
    </div>
  );
}

function KPICard({ icon, title, value, change, color }: { 
  icon: React.ReactNode; 
  title: string; 
  value: string; 
  change: string;
  color: 'blue' | 'green' | 'purple' | 'pink';
}) {
  const colorMap = {
    blue: 'from-blue-500/20 to-blue-600/10 border-blue-500/30',
    green: 'from-green-500/20 to-green-600/10 border-green-500/30',
    purple: 'from-purple-500/20 to-purple-600/10 border-purple-500/30',
    pink: 'from-pink-500/20 to-pink-600/10 border-pink-500/30',
  };

  return (
    <div className={`bg-gradient-to-br ${colorMap[color]} border rounded-xl p-6 backdrop-blur-sm`}>
      <div className="flex items-start justify-between mb-4">
        <div className="w-10 h-10 rounded-lg bg-gray-800 flex items-center justify-center">
          {icon}
        </div>
        <span className="text-xs font-semibold text-green-400">{change}</span>
      </div>
      <p className="text-gray-400 text-sm mb-1">{title}</p>
      <p className="text-2xl font-bold">{value}</p>
    </div>
  );
}

function Alert({ type, title, message }: { 
  type: 'warning' | 'info' | 'success'; 
  title: string; 
  message: string;
}) {
  const typeMap = {
    warning: { bg: 'bg-yellow-500/10', border: 'border-yellow-500/30', text: 'text-yellow-300' },
    info: { bg: 'bg-blue-500/10', border: 'border-blue-500/30', text: 'text-blue-300' },
    success: { bg: 'bg-green-500/10', border: 'border-green-500/30', text: 'text-green-300' },
  };

  const styles = typeMap[type];

  return (
    <div className={`${styles.bg} border ${styles.border} rounded-lg p-3`}>
      <p className={`font-semibold text-sm ${styles.text}`}>{title}</p>
      <p className="text-gray-400 text-xs mt-1">{message}</p>
    </div>
  );
}
