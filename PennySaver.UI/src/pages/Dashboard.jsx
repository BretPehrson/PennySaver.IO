import React from "react";
import { useQuery } from "@tanstack/react-query";
import { getDashboardOverview } from "../api/dashboard";

export default function Dashboard()
{
    const {
        data: financials,
        isLoading,
        error
    } = useQuery({
        queryKey: ["dashboardOverview"],
        queryFn: getDashboardOverview,
    });

    const formatCurrency = (amount) => {
        return new Intl.NumberFormat("en-US", {
            style: "currency",
            currency: "USD"
        }).format(amount);
    };

    return (
        <div className="p-6 max-w-7xl mx-auto">
            <h1 className="text-3xl font-bold text-gray-900 tracking-tight">Dashboard</h1>
            <p className="mt-2 text-sm text-gray-600">Welcome to PennySaver.IO. Here is your financial overview.</p>

            {error && (
                <div className="mt-4 p-4 bg-red-50 text-red-700 rounded-lg border border-red-100 text-sm">
                ⚠️ Could not load recent data: {error.message}. Please try again later.
                </div>
            )}

            {/* Quick stats grid */}
            <div className="mt-6 grid grid-cols-1 gap-5 sm:grid-cols-3">
                {/* Total Cash Card */}
                <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm transition-all hover:shadow-md">
                <span className="text-sm font-medium text-gray-500">Total Cash</span>
                    <div className="mt-2 text-2xl font-semibold text-gray-900">
                        {isLoading ? (
                        <div className="h-8 w-28 bg-gray-200 animate-pulse rounded" />
                        ) : (
                        formatCurrency(financials?.totalCash)
                        )}
                    </div>
                </div>

                {/* Monthly Budget Card */}
                <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm transition-all hover:shadow-md">
                    <span className="text-sm font-medium text-gray-500">This Month's Budget</span>
                    <div className="mt-2 text-2xl font-semibold text-gray-900">
                        {isLoading ? (
                        <div className="h-8 w-24 bg-gray-200 animate-pulse rounded" />
                        ) : (
                        formatCurrency(financials?.monthlyBudget)
                        )}
                    </div>
                </div>

                {/* Remaining Budget Card */}
                <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm transition-all hover:shadow-md">
                    <span className="text-sm font-medium text-gray-500">Remaining</span>
                    <div className={`mt-2 text-2xl font-semibold ${(financials?.remainingBudget ?? 0) < 0 ? 'text-rose-600' : 'text-emerald-600'}`}>
                        {isLoading ? (
                        <div className="h-8 w-24 bg-gray-200 animate-pulse rounded" />
                        ) : (
                        formatCurrency(financials?.remainingBudget)
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}