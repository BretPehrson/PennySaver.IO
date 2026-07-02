import React from "react";

export default function Dashboard()
{
    return(
        <div>
        <h1 className="text-3xl font-bold text-gray-900 tracking-tight">Dashboard</h1>
        <p className="mt-2 text-sm text-gray-600">Welcome to PennySaver.IO. Here is your financial overview.</p>
        
        {/* Quick stats placeholder row */}
        <div className="mt-6 grid grid-cols-1 gap-5 sm:grid-cols-3">
            <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm">
            <span className="text-sm font-medium text-gray-500">Total Cash</span>
            <div className="mt-2 text-2xl font-semibold text-gray-900">$12,450.00</div>
            </div>
            <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm">
            <span className="text-sm font-medium text-gray-500">This Month's Budget</span>
            <div className="mt-2 text-2xl font-semibold text-gray-900">$3,200.00</div>
            </div>
            <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm">
            <span className="text-sm font-medium text-gray-500">Remaining</span>
            <div className="mt-2 text-2xl font-semibold text-emerald-600">$1,150.00</div>
            </div>
        </div>
        </div>
    )
};