import React, { useState, useEffect } from 'react';
import { Card, Typography, Descriptions, Table, Tag, Space, Button, Divider, Spin, message, DescriptionsProps } from 'antd';
import { UserOutlined, MailOutlined, IdcardOutlined, TeamOutlined, EditOutlined, SafetyCertificateOutlined, HistoryOutlined } from '@ant-design/icons';
import { useSelector } from 'react-redux';
import { RootState } from '../store';
import axiosClient from '../api/axiosClient';

const { Title, Text } = Typography;

const SettingsPage: React.FC = () => {
    const { user: authUser } = useSelector((state: RootState) => state.auth);
    const [profile, setProfile] = useState<any>(null);
    const [activities, setActivities] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);
    const [actLoading, setActLoading] = useState(false);

    useEffect(() => {
        const fetchProfile = async () => {
            try {
                const data = await axiosClient.get('/auth/profile');
                setProfile(data);
            } catch (error) {
                message.error('Không thể tải thông tin cá nhân');
            } finally {
                setLoading(false);
            }
        };

        const fetchActivities = async () => {
            setActLoading(true);
            try {
                // Fetch plagiarism history as "Recent Activity"
                // Match the backend route and correct the path if needed
                const response: any = await axiosClient.get('/Plagiarism/history?limit=5');
                setActivities(response);
            } catch (error) {
                console.error("Failed to fetch activities", error);
            } finally {
                setActLoading(false);
            }
        };

        fetchProfile();
        fetchActivities();
    }, []);

    const activityColumns = [
        {
            title: 'Hoạt động',
            dataIndex: 'sourceDocumentTitle', // Fixed field name
            key: 'sourceDocumentTitle',
            render: (text: string) => (
                <Space>
                    <HistoryOutlined />
                    <Text>Kiểm tra đạo văn: <Text strong>{text || 'Tài liệu không tên'}</Text></Text>
                </Space>
            )
        },
        {
            title: 'Kết quả',
            dataIndex: 'overallSimilarityPercentage', // Fixed field name
            key: 'overallSimilarityPercentage',
            render: (score: number) => (
                <Tag color={score > 50 ? 'red' : score > 20 ? 'orange' : 'green'}>
                    {(score || 0).toFixed(1)}% tương đồng
                </Tag>
            )
        },
        {
            title: 'Thời gian',
            dataIndex: 'checkDate',
            key: 'checkDate',
            render: (date: string) => date ? new Date(date).toLocaleString('vi-VN') : 'N/A' // Remove dayjs dependency
        }
    ];

    if (loading) {
        return (
            <div style={{ textAlign: 'center', padding: '100px' }}>
                <Spin size="large" tip="Đang tải thông tin..." />
            </div>
        );
    }

    const items: DescriptionsProps['items'] = [
        {
            key: '1',
            label: <Space><UserOutlined /> Họ và tên</Space>,
            children: <Text strong>{profile?.fullName}</Text>,
        },
        {
            key: '2',
            label: <Space><MailOutlined /> Email</Space>,
            children: profile?.email,
        },
        {
            key: '3',
            label: <Space><IdcardOutlined /> Tên đăng nhập</Space>,
            children: <Tag color="blue">{profile?.username}</Tag>,
        },
        {
            key: '4',
            label: <Space><TeamOutlined /> Chức vụ</Space>,
            children: <Tag color={profile?.role === 'Admin' ? 'red' : 'green'}>{profile?.role}</Tag>,
        },
        {
            key: '5',
            label: <Space><TeamOutlined /> Khoa/Phòng</Space>,
            children: profile?.facultyName || 'N/A',
        },
        {
            key: '6',
            label: <Space><TeamOutlined /> Chuyên ngành</Space>,
            children: profile?.departmentName || 'N/A',
        },
    ];

    return (
        <div className="animate-fade-in" style={{ maxWidth: 1000, margin: '0 auto' }}>
            <Title level={2} className="gradient-text">Cài đặt tài khoản</Title>
            <Text type="secondary" style={{ marginBottom: 24, display: 'block' }}>
                Quản lý thông tin cá nhân và thiết lập hệ thống
            </Text>

            <Card className="glass-card" bordered={false}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
                    <Title level={4} style={{ margin: 0 }}>Thông tin cá nhân</Title>
                    <Button type="primary" icon={<EditOutlined />}>Chỉnh sửa</Button>
                </div>

                <Descriptions
                    bordered
                    column={{ xxl: 2, xl: 2, lg: 2, md: 1, sm: 1, xs: 1 }}
                    items={items}
                />

                <Divider />

                <Title level={4}>Bảo mật</Title>
                <Card type="inner" title={<Space><SafetyCertificateOutlined /> Bảo mật tài khoản</Space>}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <div>
                            <Text strong>Mật khẩu</Text>
                            <br />
                            <Text type="secondary">Thay đổi mật khẩu định kỳ để bảo vệ tài khoản của bạn</Text>
                        </div>
                        <Button>Đổi mật khẩu</Button>
                    </div>
                </Card>
            </Card>

            <Card className="glass-card" bordered={false} style={{ marginTop: 24 }}>
                <Title level={4}>Hoạt động gần đây</Title>
                <Text type="secondary">Danh sách các lần kiểm tra đạo văn và tương tác gần nhất</Text>

                <Table
                    loading={actLoading}
                    dataSource={activities}
                    columns={activityColumns}
                    pagination={false}
                    rowKey="id"
                    style={{ marginTop: 16 }}
                    locale={{ emptyText: 'Chưa có hoạt động nào được ghi nhận' }}
                />
            </Card>
        </div>
    );
};

export default SettingsPage;
