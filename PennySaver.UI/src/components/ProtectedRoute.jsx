import React from "react";
import { Link, Outlet } from "react-router-dom";

const ProtectedRoute = () => {
    const token = localStorage.getItem("token");

    if (!token) {
        return (
            <div className="flex min-h-screen items-center justify-center bg-slate-950 px-4">
                <div className="w-full max-w-md rounded-2xl bg-slate-900 p-8 shadow-xl border border-slate-800 text-center flex flex-col items-center">
                    
                    {/* Modern Shield/Warning Icon */}
                    <div className="w-14 h-14 bg-red-500/10 text-red-400 rounded-full flex items-center justify-center text-xl border border-red-500/20 mb-5 animate-pulse">
                        <svg xmlns="http://w3.org" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" className="w-6 h-6">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 1 0-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 0 0 2.25-2.25v-6.75a2.25 2.25 0 0 0-2.25-2.25H6.75a2.25 2.25 0 0 0-2.25 2.25v6.75a2.25 2.25 0 0 0 2.25 2.25Z" />
                        </svg>
                    </div>
                    
                    {/* Text Context */}
                    <h1 className="text-2xl font-extrabold tracking-tight text-slate-100">
                        Access Denied
                    </h1>
                    
                    <p className="mt-3 text-sm text-slate-400 max-w-xs leading-relaxed">
                        You do not have permission to view this page. Please sign in to access your PennySaver account.
                    </p>
                    
                    {/* Action Button matching login theme */}
                    <Link 
                        to="/login" 
                        onClick={() => { localStorage.clear(); sessionStorage.clear(); }} 
                        className="mt-6 w-full rounded-lg bg-emerald-500 py-3 font-semibold text-slate-950 transition-colors hover:bg-emerald-400 focus:outline-none text-center text-sm shadow-lg shadow-emerald-500/10"
                    >
                        Go to Sign In
                    </Link>
                </div>
            </div>
        );
    }

    return <Outlet />;
}

export default ProtectedRoute;