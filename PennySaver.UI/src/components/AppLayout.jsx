import React, { useState } from "react";
import { Link, Outlet } from "react-router-dom";
export default function AppLayout({ logout }) {

const [isSidebarOpen, setIsSidebarOpen] = useState(false);

const toggleSidebar = () => setIsSidebarOpen(!isSidebarOpen);

return (
  <div className="w-full min-h-screen bg-gray-50 flex overflow-hidden relative">
      
    {/* Mobile Background Overlay (Dims screen when sidebar is open on mobile) */}
    {isSidebarOpen && (
      <div 
        className="fixed inset-0 bg-black/50 z-40 md:hidden" 
        onClick={toggleSidebar}
      />
    )}

    {/* Left Sidebar Navigation */}
    <aside className={`fixed inset-y-0 left-0 z-50 w-64 bg-gray-900 text-gray-100 flex flex-col justify-between border-r border-gray-800 transition-transform duration-300 ease-in-out
      ${isSidebarOpen ? "translate-x-0" : "-translate-x-full"} 
      md:relative md:translate-x-0 md:flex shrink-0`}>
        <div className="w-full">
          {/* Brand/Logo Area */}
          <div className="h-16 flex items-center justify-between px-6 border-b border-gray-800">
            <span className="text-xl font-bold tracking-wider text-green-400 block">
              PennySaver.IO
            </span>
            {/* Mobile Close Button (X) inside sidebar */}
            <button onClick={toggleSidebar} className="text-gray-400 hover:text-white md:hidden">
              ✕
            </button>
          </div>
          
          {/* Menu Items */}
          <nav className="mt-6 px-4 flex flex-col gap-y-1">
            <Link to="/dashboard" onClick={() => setIsSidebarOpen(false)} className="block px-4 py-2.5 text-sm font-medium rounded-lg text-gray-300 hover:bg-gray-800 hover:text-white transition-colors">
              Overview Dashboard
            </Link>
            <Link to="/budgets" onClick={() => setIsSidebarOpen(false)} className="block px-4 py-2.5 text-sm font-medium rounded-lg text-gray-300 hover:bg-gray-800 hover:text-white transition-colors">
              Budget Categories
            </Link>
            <Link to="/accounts" onClick={() => setIsSidebarOpen(false)} className="block px-4 py-2.5 text-sm font-medium rounded-lg text-gray-300 hover:bg-gray-800 hover:text-white transition-colors">
              Bank Accounts
            </Link>
          </nav>
        </div>

        {/* Bottom User Area */}
        <div className="p-4 border-t border-gray-800 w-full">
          <button onClick={logout} className="w-full block text-center px-4 py-2 text-sm font-medium text-gray-300 bg-gray-800 rounded-lg hover:bg-red-900 hover:text-white transition-all cursor-pointer">
            Sign Out Session
          </button>
        </div>
      </aside>

      {/* Main Dynamic Workspace Content View */}
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        
        {/* Top Minimalist Header */}
        <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between md:justify-end px-4 md:px-8 shadow-sm">
          
          {/* Hamburger Button (Visible ONLY on mobile) */}
          <button 
            onClick={toggleSidebar} 
            className="p-2 rounded-md text-gray-600 hover:bg-gray-100 md:hidden"
            aria-label="Open Menu"
          >
            <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>

          {/* User Profile Info */}
          <div className="flex items-center space-x-3">
            <span className="text-sm font-medium text-gray-600">Welcome back</span>
            <div className="w-8 h-8 rounded-full bg-slate-800 flex items-center justify-center text-xs font-semibold text-emerald-400 border border-slate-700">
                BP
            </div>
          </div>
        </header>

        {/* Dynamic Inner Body Area */}
        <main className="flex-1 p-8 overflow-y-auto max-w-7xl w-full mx-auto">
          <Outlet />
        </main>
      </div>

    </div>
  );
}