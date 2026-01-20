import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { RootState } from './store';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardLayout from './components/layout/DashboardLayout';
import HomePage from './pages/HomePage';
import PlagiarismCheckPage from './pages/PlagiarismCheckPage';
import DocumentListPage from './pages/DocumentListPage';
import UserManagementPage from './pages/UserManagementPage';
import SettingsPage from './pages/SettingsPage';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import ResetPasswordPage from './pages/ResetPasswordPage';

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
    const { isAuthenticated } = useSelector((state: RootState) => state.auth);
    return isAuthenticated ? <>{children}</> : <Navigate to="/login" />;
};

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                <Route path="/reset-password" element={<ResetPasswordPage />} />

                <Route path="/" element={
                    <ProtectedRoute>
                        <DashboardLayout />
                    </ProtectedRoute>
                }>
                    <Route index element={<HomePage />} />
                    <Route path="check" element={<PlagiarismCheckPage />} />
                    <Route path="results/:id" element={<PlagiarismCheckPage />} />
                    <Route path="documents" element={<DocumentListPage />} />
                    <Route path="users" element={<UserManagementPage />} />
                    <Route path="settings" element={<SettingsPage />} />
                </Route>

                <Route path="*" element={<Navigate to="/" />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;
