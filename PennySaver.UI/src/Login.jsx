import {useState} from "react";
import axios from "axios";
import api from "./api";

function Login() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setLoading(false);
        setError("");

        try {
            setLoading(true);

            const response = await api.post(
                "/auth/issuetoken",
                { email, password }
            );

            const { token } = response.data;
            localStorage.setItem("token", token);

            alert("Login successful!");
        }
        catch (err) {
            const serverMessage = err.response?.data?.message || "An error occurred during login.";
            setError(serverMessage);
        }
        finally {
            setLoading(false);
        }
    };

    return (
    <div className="flex min-h-screen items-center justify-center bg-slate-950 px-4">
      <div className="w-full max-w-md rounded-2xl bg-slate-900 p-8 shadow-xl border border-slate-800">
        
        {/* Header */}
        <div className="text-center">
          <h2 className="text-3xl font-extrabold tracking-tight text-emerald-400">
            PennySaver.IO
          </h2>
          <p className="mt-2 text-sm text-slate-400">Welcome back! Please sign in.</p>
        </div>

        {/* Error Alert Box */}
        {error && (
          <div className="mt-4 rounded-lg bg-red-500/10 p-3 text-sm text-red-400 border border-red-500/20">
            {error}
          </div>
        )}

        {/* Form */}
        <form className="mt-6 space-y-4" onSubmit={handleSubmit}>
          <div>
            <label className="text-sm font-medium text-slate-300">Email Address</label>
            <input
              type="email"
              required
              className="mt-1 w-full rounded-lg bg-slate-950 border border-slate-800 p-3 text-white placeholder-slate-500 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
              placeholder="you@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)} // Binds input value to state
            />
          </div>

          <div>
            <label className="text-sm font-medium text-slate-300">Password</label>
            <input
              type="password"
              required
              className="mt-1 w-full rounded-lg bg-slate-950 border border-slate-800 p-3 text-white placeholder-slate-500 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>

          <button type="submit" disabled={loading} className="w-full rounded-lg bg-emerald-500 py-3 font-semibold text-slate-950 transition-colors hover:bg-emerald-400 focus:outline-none disabled:opacity-50"          >
            {loading ? "Signing in..." : "Sign In"}
          </button>
        </form>

      </div>
    </div>
  );
}

export default Login;