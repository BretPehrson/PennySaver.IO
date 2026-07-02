import React from "react";
import { Link, Outlet } from "react-router-dom";

const ProtectedRoute = () => {
    const token = localStorage.getItem("token");

    if (!token) {
        return (
            <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-6 text-center">
            <div className="w-16 h-16 bg-red-100 text-red-600 rounded-full flex items-center justify-center text-2xl font-bold mb-4">
            !
            </div>
            <h1 className="text-2xl font-bold text-gray-900 tracking-tight">Access Denied</h1>
            <p className="mt-2 text-sm text-gray-600 max-w-sm">
            You do not have permission to view this page. Please sign in to access your PennySaver account.
            </p>
            <Link to="/login" className="mt-6 px-4 py-2 bg-gray-900 text-white rounded-lg text-sm font-medium hover:bg-gray-800 transition-colors">
            Go to Sign In
            </Link>
        </div>
        );
    }

    return <Outlet />;
}

export default ProtectedRoute;