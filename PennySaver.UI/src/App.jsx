import React from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import Login from "./Login";
import ProtectedRoute from "./components/ProtectedRoute";
import AppLayout from "./components/AppLayout";

import Dashboard from "./pages/Dashboard";
import Budgets from "./pages/Budgets";
import Accounts from "./pages/Accounts";

function App() {
    return (
        <Router>
            <Routes>
                <Route path="/login" element={<Login />} />
                <Route element={<ProtectedRoute />}>
                    <Route element={<AppLayout />}>
                        <Route path="/dashboard" element={<Dashboard />} />
                        <Route path="/budgets" element={<Budgets />} />
                        <Route path="/accounts" element={<Accounts />} />
                    </Route>
                </Route>
            </Routes>
        </Router>
    );
}

export default App;