import api from "./client";

export const getBudgets = async () => {
    const response = await api.get("/budget");
    return response.data;
};