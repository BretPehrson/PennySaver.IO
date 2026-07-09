import axios from "axios";

const api = axios.create({
    baseURL: "http://localhost:5295/api",
    withCredentials: true,
});

api.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem("token");

        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }

        return config;
    },
    (error) => Promise.reject(error)
);

api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;
        
            try {
                const resonse = await axios.post(
                    "http://localhost:5295/api/auth/refresh",
                    {},
                    { withCredentials: true }   
                );

                const { token } = resonse.data;
                localStorage.setItem("token", token);

                originalRequest.headers.Authorization = `Bearer ${token}`;
                return api(originalRequest);
            }
            catch (refreshError) {
                console.error("Refresh token expired Logging out...", refreshError);
                localStorage.removeItem("token");
                window.location.href = "/login";
                return Promise.reject(refreshError);   
            }
        }
    return Promise.reject(error);
    }
);

export const logout = async () => {
    try {
        await api.post("/auth/logout");
    }
    catch (err) {
        console.error("Error during logout:", err);
    }
    finally {
        localStorage.removeItem("token");
        window.location.href = "/login";
    }
};

export default api;