import React, { useState } from 'react';
import { Layout, Menu, Button, Avatar, Dropdown, Space, Typography } from 'antd';
import {
    DashboardOutlined,
    FileSearchOutlined,
    FileTextOutlined,
    UserOutlined,
    LogoutOutlined,
    MenuUnfoldOutlined,
    MenuFoldOutlined,
    SettingOutlined,
    BellOutlined
} from '@ant-design/icons';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '../../store';
import { logout } from '../../store/slices/authSlice';

const { Header, Sider, Content } = Layout;
const { Text } = Typography;

const DashboardLayout: React.FC = () => {
    const [collapsed, setCollapsed] = useState(false);
    const navigate = useNavigate();
    const location = useLocation();
    const dispatch = useDispatch();
    const { user } = useSelector((state: RootState) => state.auth);

    const handleLogout = () => {
        dispatch(logout());
        navigate('/login');
    };

    const userMenuItems = [
        { key: 'profile', icon: <UserOutlined />, label: 'Thông tin cá nhân', onClick: () => navigate('/settings') },
        { key: 'settings', icon: <SettingOutlined />, label: 'Cài đặt', onClick: () => navigate('/settings') },
        { type: 'divider' },
        { key: 'logout', icon: <LogoutOutlined />, label: 'Đăng xuất', onClick: handleLogout },
    ];

    return (
        <Layout style={{ minHeight: '100vh' }}>
            <Sider
                trigger={null}
                collapsible
                collapsed={collapsed}
                breakpoint="lg"
                collapsedWidth={window.innerWidth < 992 ? 0 : 80}
                onBreakpoint={(broken) => {
                    if (broken) setCollapsed(true);
                }}
                theme="light"
                width={260}
                style={{
                    boxShadow: '4px 0 10px rgba(0,0,0,0.02)',
                    zIndex: 1001,
                    position: window.innerWidth < 992 ? 'fixed' : 'relative',
                    height: '100vh'
                }}
            >
                <div
                    style={{
                        height: 70,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        borderBottom: '1px solid #f0f0f0',
                        cursor: 'pointer'
                    }}
                    onClick={() => navigate(user?.role === 'Student' ? '/check' : '/')}
                >
                    <div style={{
                        width: 32, height: 32, background: '#003a8c', borderRadius: 6,
                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                        color: 'white', fontWeight: 'bold'
                    }}>
                        B
                    </div>
                    {!collapsed && <span style={{ marginLeft: 12, fontWeight: 700, fontSize: 16, color: '#003a8c' }}>BAV PLAGIARISM</span>}
                </div>
                <Menu
                    theme="light"
                    mode="inline"
                    selectedKeys={[location.pathname]}
                    style={{ borderRight: 0, padding: '16px 8px' }}
                    items={[
                        // Hide Dashboard for Students
                        ...(user?.role !== 'Student' ? [{
                            key: '/',
                            icon: <DashboardOutlined />,
                            label: 'Tổng quan',
                            onClick: () => navigate('/')
                        }] : []),
                        {
                            key: '/check',
                            icon: <FileSearchOutlined />,
                            label: 'Kiểm tra đạo văn',
                            onClick: () => navigate('/check')
                        },
                        {
                            key: '/documents',
                            icon: <FileTextOutlined />,
                            label: user?.role === 'Student' ? 'Tài liệu của tôi' : 'Kho tài liệu',
                            onClick: () => navigate('/documents')
                        },
                        // Only show User Management for Admin
                        ...(user?.role === 'Admin' ? [{
                            key: '/users',
                            icon: <UserOutlined />,
                            label: 'Quản lý người dùng',
                            onClick: () => navigate('/users')
                        }] : [])
                    ]}
                />
            </Sider>
            <Layout>
                <Header className="bav-header">
                    <Button
                        type="text"
                        icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
                        onClick={() => setCollapsed(!collapsed)}
                        style={{ fontSize: '16px', width: 40, height: 40 }}
                    />

                    <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                        <Button type="text" icon={<BellOutlined />} style={{ fontSize: 18 }} className="hide-mobile" />
                        <Dropdown menu={{ items: userMenuItems as any }} placement="bottomRight" arrow>
                            <Space style={{ cursor: 'pointer' }}>
                                <Avatar style={{ backgroundColor: '#1890ff' }} icon={<UserOutlined />} />
                                <div className="hide-mobile" style={{ display: 'flex', flexDirection: 'column', lineHeight: '1.2' }}>
                                    <Text strong>{user?.fullName}</Text>
                                    <Text type="secondary" style={{ fontSize: 12 }}>{user?.role}</Text>
                                </div>
                            </Space>
                        </Dropdown>
                    </div>
                </Header>
                <Content style={{
                    margin: window.innerWidth < 768 ? '16px 12px' : '24px',
                    minHeight: 280,
                    position: 'relative'
                }}>
                    <Outlet />
                </Content>
            </Layout>
            {window.innerWidth < 992 && !collapsed && (
                <div
                    style={{
                        position: 'fixed',
                        top: 0,
                        left: 0,
                        right: 0,
                        bottom: 0,
                        background: 'rgba(0,0,0,0.3)',
                        zIndex: 1000
                    }}
                    onClick={() => setCollapsed(true)}
                />
            )}
        </Layout>
    );
};

export default DashboardLayout;
