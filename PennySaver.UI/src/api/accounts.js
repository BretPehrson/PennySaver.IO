import api from "./client";

export const getAccounts = async () => {
    const response = await api.get("/account");
    return response.data;
};

export const createAccount = async (accountData) => {
    const response = await api.post("/account", accountData);
    return response.data;
}

export const deleteAccount = async (accountId) => {
    await api.delete(`/account/${accountId}`);
};

export const updateAccount = async (accountId, accountData) => {
    const response = await api.put(`/account/${accountId}`, accountData);
    return response.data;
}