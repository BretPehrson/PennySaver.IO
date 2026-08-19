import React, { useState, useEffect, useCallback } from "react";
import { usePlaidLink } from "react-plaid-link";
import { getAccounts, createAccount, updateAccount, deleteAccount } from "../api/accounts";
import { createLinkToken, exchangePublicToken } from "../api/plaid";

export default function Accounts() {
  const [accounts, setAccounts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Modal & Form State Management
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingId, setEditingId] = useState(null); // Track which account is being edited (null if creating new)
  const [formData, setFormData] = useState({ accountName: "", institution: "", type: 0, balance: "" });
  const [submitError, setSubmitError] = useState("");

  const [deletingId, setDeletingId] = useState(null); // Track which account is in "delete confirmation" mode

  const [linkToken, setLinkToken] = useState(null);
  const [plaidError, setPlaidError] = useState("");

  const ACCOUNT_TYPE_LABELS = [
    "Checking",
    "Savings",
    "Credit Card",
    "Investment",
    "Loan"
  ];

  // Fetch list data on mount
  const fetchAccountData = async () => {
    try {
      setLoading(true);
      const data = await getAccounts();
      setAccounts(data);
    } catch (err) {
      setError("Unable to connect to backend server.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchAccountData(); }, []);

  const totalAssets = accounts
    .filter(a => a.type == 0 || a.type == 1 || a.type == 3) // Only include Checking, Savings, and Investment in asset total
    .reduce((sum, a) => sum + a.balance, 0);

  const totalLiabilities = accounts
    .filter(a => a.type == 2 || a.type == 4) // Only include Credit Card and Loan in liability total
    .reduce((sum, a) => sum + a.balance, 0);

  const netWorth = totalAssets - totalLiabilities;

  const handleOpenEdit = (account) => {
    setEditingId(account.id);
    setFormData({
      accountName: account.accountName,
      institution: account.institution || "",
      type: account.type,
      balance: account.balance.toString() // Convert to string for the input form element
    });
    setIsModalOpen(true);
  };

  const handleOpenCreate = () => {
    setEditingId(null);
    setFormData({ accountName: "", institution: "", type: 0, balance: "" });
    setIsModalOpen(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.accountName || !formData.balance) {
      setSubmitError("Name and Initial Balance are required fields.");
      return;
    }

    try {
      setSubmitError("");
      const payload = {
        accountName: formData.accountName,
        institution: formData.institution || "",
        type: Number(formData.type),
        balance: parseFloat(formData.balance)
        };

      console.log("Submitting Account Creation with Payload:", payload);

      if (editingId) {
        console.log("Sending PUT request for ID:", editingId); // Add this to debug!
        await updateAccount(editingId, payload);
      } else {
        console.log("Sending POST request (Create Mode)");
        await createAccount(payload);
      }
      
      // Reset state on success
      setIsModalOpen(false);
      setFormData({ accountName: "", institution: "", type: 0, balance: "" });
      fetchAccountData(); // Re-trigger GET to display your new card instantly!
    } catch (err) {
      console.error("Full Axios Error Object:", err);
      console.error("Server Response Data:", err.response?.data);

      const backendErrorMsg = err.response?.data?.message || JSON.stringify(err.response?.data) || "An error occurred while saving the account.";
      setSubmitError(backendErrorMsg);
    }
  };

  const handleDelete = async (accountId) => {
    try {
      await deleteAccount(accountId);
      setDeletingId(null);
      fetchAccountData(); // Refresh list after deletion
    } catch (err) {
      console.log("Error deleting account:", err);
      alert(`Error deleting account. Please try again. ${err.message}`);
    } 
  };

  const onPlaidSuccess = useCallback(async (publicToken, metadata) => {
    try {
      setPlaidError("");
      await exchangePublicToken({
        publicToken,
        plaidAccountId: metadata.accounts[0]?.id ?? "",
        institutionName: metadata.institution?.name ?? "Unknown Institution",
        institutionId: metadata.institution?.institution_id ?? "",
      });
      setLinkToken(null);
      fetchAccountData(); // Pull in the newly synced accounts
    } catch (err) {
      setPlaidError(err.response?.data?.message || "Failed to link your bank account. Please try again.");
    }
  }, []);

  const { open: openPlaidLink, ready: plaidLinkReady } = usePlaidLink({
    token: linkToken,
    onSuccess: onPlaidSuccess,
  });

  // Auto-open Link as soon as a fresh token is ready
  useEffect(() => {
    if (linkToken && plaidLinkReady) openPlaidLink();
  }, [linkToken, plaidLinkReady, openPlaidLink]);

  const handleConnectBank = async () => {
    try {
      setPlaidError("");
      const token = await createLinkToken();
      setLinkToken(token);
    } catch (err) {
      setPlaidError("Unable to start Plaid Link. Please try again.");
    }
  };

  const formatCurrency = (value) => {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
  };

  if (loading) return <div className="flex justify-center items-center h-64 text-gray-500 animate-pulse">Loading profiles...</div>;
  if (error) return <div className="p-4 bg-red-50 text-red-700 rounded-xl">{error}</div>;

return (
    <div className="space-y-8">
      {/* Page Header View */}
      <div className="sm:flex sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 tracking-tight">Bank Accounts</h1>
          <p className="mt-1 text-sm text-gray-500">Monitor ledger profiles and cross-platform balances.</p>
        </div>
        <div className="mt-4 sm:mt-0 flex items-center gap-2">
          <button 
            onClick={handleConnectBank}
            className="px-4 py-2 text-sm font-medium text-slate-900 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors shadow-sm cursor-pointer"
          >
            🏦 Connect Bank (Plaid)
          </button>
          <button 
            onClick={handleOpenCreate}
            className="px-4 py-2 text-sm font-medium text-white bg-slate-900 rounded-lg hover:bg-slate-800 transition-colors shadow-sm cursor-pointer"
          >
            Add Account
          </button>
        </div>
      </div>

      {plaidError && <div className="p-3 bg-red-50 text-red-700 text-sm rounded-xl">{plaidError}</div>}

      {/* Quick-Stats Aggregation Ribbons */}
      {accounts.length > 0 && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div className="bg-white p-5 rounded-xl border border-gray-100 shadow-xs">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Liquid Assets</p>
            <p className="mt-2 text-xl sm:text-2xl font-bold text-emerald-600 break-words">{formatCurrency(totalAssets)}</p>
          </div>
          <div className="bg-white p-5 rounded-xl border border-gray-100 shadow-xs">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Liabilities / Debt</p>
            <p className="mt-2 text-xl sm:text-2xl font-bold text-red-600 break-words">{formatCurrency(totalLiabilities)}</p>
          </div>
          <div className="bg-white p-5 rounded-xl border border-gray-100 shadow-xs bg-slate-50/50">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Net Ledger Balance</p>
            <p className={`mt-2 text-xl sm:text-2xl font-bold ${netWorth >= 0 ? "text-slate-900" : "text-red-700"} break-words`}>
              {formatCurrency(netWorth)}
            </p>
          </div>
        </div>
      )}

      {/* Main Accounts Grid Worksheets */}
      {accounts.length === 0 ? (
        <div className="text-center p-12 bg-white rounded-xl border border-dashed border-gray-300">
          <h3 className="text-sm font-medium text-gray-900">No accounts connected</h3>
          <p className="mt-1 text-sm text-gray-500">Click Add Account above to begin setup.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {accounts.map((account) => (
            <div key={account.id} className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm flex flex-col justify-between hover:shadow-md transition-all relative group">
              
              <div>
                <div className="flex justify-between items-start">
                  <span className={`text-[10px] px-2 py-0.5 font-bold tracking-wider rounded-md uppercase ${
                    account.type === 2 || account.type === 4 ? "bg-red-50 text-red-700" : "bg-emerald-50 text-emerald-700"
                  }`}>
                    {ACCOUNT_TYPE_LABELS[account.type] || "Unknown"}
                  </span>
                  
                  {/* Action Cluster Buttons */}
                  <div className="flex items-center space-x-2">
                    {account.isAutomated ? (
                      /* Locked Indicator for Plaid / Synced accounts */
                      <span 
                        className="text-[10px] font-medium bg-gray-100 text-gray-500 px-2 py-0.5 rounded-md cursor-help flex items-center gap-1"
                        title="This account automatically syncs data. Core values cannot be edited or deleted manually."
                      >
                        🔒 Synced
                      </span>
                    ) : deletingId === account.id ? (
                      /* Confirm Delete Mode (Manual Accounts Only) */
                      <div className="flex items-center space-x-2 animate-in fade-in slide-in-from-right-2 duration-100">
                        <button onClick={() => handleDelete(account.id)} className="text-xs font-bold text-red-600 hover:underline cursor-pointer">Confirm</button>
                        <span className="text-gray-300 text-xs">|</span>
                        <button onClick={() => setDeletingId(null)} className="text-xs text-gray-400 hover:text-gray-600 cursor-pointer">Cancel</button>
                      </div>
                    ) : (
                      /* Normal Edit/Delete Mode (Manual Accounts Only) */
                      <div className="flex items-center space-x-1 shrink-0">
                        {/* Edit Action Trigger */}
                        <button 
                          onClick={() => handleOpenEdit(account)}
                          className="text-gray-400 hover:text-blue-500 p-1 text-xs cursor-pointer"
                          title="Edit account details"
                        >
                          ✏️
                        </button>
                        {/* Delete Action Trigger */}
                        <button 
                          onClick={() => setDeletingId(account.id)}
                          className="text-gray-400 hover:text-red-500 p-1 text-xs cursor-pointer"
                          title="Delete account profile"
                        >
                          🗑️
                        </button>
                      </div>
                    )}
                  </div>
                </div>
                
                <h3 className="mt-2 text-lg font-bold text-gray-900 tracking-tight">{account.accountName}</h3>
                <p className="text-xs text-gray-400 mt-0.5">{account.institution || "Local Portfolio Ledger"}</p>
              </div>

              <div className="mt-6 pt-4 border-t border-gray-50 flex flex-col gap-y-1">
                <span className="text-xs text-gray-400 font-semibold uppercase tracking-wider">
                  Available Balance
                </span>
                <span className={`text-2xl font-bold leading-tight ${account.type === 2 || account.type === 4 ? "text-red-600" : "text-gray-900"} break-words`}>
                  {formatCurrency(account.balance)}
                </span>
              </div>

            </div>
          ))}
        </div>
      )}

      {/* DYNAMIC CREATE/EDIT ENTRY MODAL OVERLAY */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="bg-white rounded-xl max-w-md w-full shadow-xl border border-gray-100 overflow-hidden animate-in fade-in zoom-in-95 duration-150">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              {/* Modal Title changes dynamically based on active mode context */}
              <h2 className="text-lg font-bold text-gray-900">
                {editingId ? "Modify Account Details" : "Link New Bank Account"}
              </h2>
              <button onClick={() => setIsModalOpen(false)} className="text-gray-400 hover:text-gray-600 font-semibold text-lg cursor-pointer">✕</button>
            </div>

            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              {submitError && <div className="p-2 bg-red-50 text-red-700 text-xs rounded-lg">{submitError}</div>}

              <div>
                <label className="block text-xs font-semibold text-gray-500 uppercase tracking-wider">Account Profile Name</label>
                <input 
                  type="text" required placeholder="e.g., Primary Checking"
                  value={formData.accountName} onChange={(e) => setFormData({...formData, accountName: e.target.value})}
                  className="mt-1.5 w-full rounded-lg border border-gray-200 px-3 py-2 text-sm outline-none focus:border-slate-900"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-500 uppercase tracking-wider">Financial Institution</label>
                <input 
                  type="text" placeholder="e.g., Chase, Wells Fargo, Acorns, etc."
                  value={formData.institution}
                  onChange={(e) => setFormData({...formData, institution: e.target.value})}
                  className="mt-1.5 w-full rounded-lg border border-gray-200 px-3 py-2 text-sm outline-none focus:border-slate-900"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-gray-500 uppercase tracking-wider">Account Type</label>
                  <select 
                    value={formData.type} 
                    onChange={(e) => setFormData({...formData, type: parseInt(e.target.value, 10)})}
                    className="mt-1.5 w-full rounded-lg border border-gray-200 px-3 py-2 text-sm outline-none bg-white focus:border-slate-900"
                  >
                    <option value={0}>Checking</option>
                    <option value={1}>Savings</option>
                    <option value={2}>Credit Card</option>
                    <option value={3}>Investment</option>
                    <option value={4}>Loan</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-500 uppercase tracking-wider">Current Balance</label>
                  <input 
                    type="number" step="0.01" required placeholder="0.00"
                    value={formData.balance} onChange={(e) => setFormData({...formData, balance: e.target.value})}
                    className="mt-1.5 w-full rounded-lg border border-gray-200 px-3 py-2 text-sm outline-none focus:border-slate-900"
                  />
                </div>
              </div>

              <div className="pt-4 border-t border-gray-100 flex justify-end space-x-3">
                <button type="button" onClick={() => setIsModalOpen(false)} className="px-4 py-2 border border-gray-200 text-sm font-medium rounded-lg text-gray-600 hover:bg-gray-50 cursor-pointer">Cancel</button>
                <button type="submit" className="px-4 py-2 text-sm font-medium text-white bg-slate-900 rounded-lg hover:bg-slate-800 shadow-sm cursor-pointer">
                  {/* Button text updates automatically */}
                  {editingId ? "Save Changes" : "Save Account"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}