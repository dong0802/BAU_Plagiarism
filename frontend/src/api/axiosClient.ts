import axios, { InternalAxiosRequestConfig, AxiosResponse } from 'axios';

const axiosClient = axios.create({
    baseURL: import.meta.env.VITE_API_URL || '/api',
    headers: {
        'Content-Type': 'application/json',
    },
});

// Interceptor để thêm token vào header
axiosClient.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => Promise.reject(error)
);

// Interceptor để xử lý lỗi hệ thống/xác thực
axiosClient.interceptors.response.use(
    (response: AxiosResponse) => {
        return response.data;
    },
    (error) => {
        if (error.response?.status === 401) {
            localStorage.removeItem('token');
            localStorage.removeItem('user');

            // Chỉ chuyển hướng nếu không phải đang ở trang login để tránh lặp
            if (window.location.pathname !== '/login') {
                window.location.href = '/login';
            }
        }

        const message = error.response?.data?.message || error.message || 'Có lỗi xảy ra';
        return Promise.reject(message);
    }
);

export default axiosClient;
