import api from "./client";

export const getDashboardOverview = async () => {
    const response = await api.get("/dashboard/overview");
    return response.data;
};