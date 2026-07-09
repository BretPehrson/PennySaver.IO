import api from "./client";

export const getTransactions = async () => {
    const response = await api.get("/transaction");
    return response.data;
};