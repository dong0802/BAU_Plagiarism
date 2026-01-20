import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface User {
    id: number;
    username: string;
    fullName: string;
    role: 'Admin' | 'Lecturer' | 'Student';
    email: string;
    dailyCheckLimit: number;
    checksUsedToday: number;
    remainingChecksToday: number;
}

interface AuthState {
    user: User | null;
    token: string | null;
    isAuthenticated: boolean;
    loading: boolean;
}

const initialState: AuthState = {
    user: JSON.parse(localStorage.getItem('user') || 'null'),
    token: localStorage.getItem('token'),
    isAuthenticated: !!localStorage.getItem('token'),
    loading: false,
};

const authSlice = createSlice({
    name: 'auth',
    initialState,
    reducers: {
        setCredentials: (
            state,
            action: PayloadAction<{ user: User; token: string }>
        ) => {
            state.user = action.payload.user;
            state.token = action.payload.token;
            state.isAuthenticated = true;
            localStorage.setItem('token', action.payload.token);
            localStorage.setItem('user', JSON.stringify(action.payload.user));
        },
        logout: (state) => {
            state.user = null;
            state.token = null;
            state.isAuthenticated = false;
            localStorage.removeItem('token');
            localStorage.removeItem('user');
        },
        updateCredits: (
            state,
            action: PayloadAction<{ remainingChecksToday: number; dailyCheckLimit: number }>
        ) => {
            if (state.user) {
                state.user.remainingChecksToday = action.payload.remainingChecksToday;
                state.user.dailyCheckLimit = action.payload.dailyCheckLimit;
                localStorage.setItem('user', JSON.stringify(state.user));
            }
        },
    },
});

export const { setCredentials, logout, updateCredits } = authSlice.actions;
export default authSlice.reducer;
