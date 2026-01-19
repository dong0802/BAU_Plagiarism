import React, { useState, useEffect } from 'react';
import { Row, Col, Card, Statistic, Typography, Table, Tag, message, Spin } from 'antd';
import {
    FileTextOutlined,
    CheckCircleOutlined,
    WarningOutlined,
    TeamOutlined
} from '@ant-design/icons';
import { motion } from 'framer-motion';
import plagiarismApi, { PlagiarismStatisticsDto } from '../api/plagiarismApi';

const { Title, Text } = Typography;

const HomePage: React.FC = () => {
    const [statsData, setStatsData] = useState<PlagiarismStatisticsDto | null>(null);
    const [recentActivity, setRecentActivity] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            try {
                const [stats, history] = await Promise.all([
                    plagiarismApi.getStatistics(),
                    plagiarismApi.getHistory({ limit: 5 })
                ]);

                setStatsData(stats);
                setRecentActivity(history);
            } catch (error) {
                console.error('Error fetching dashboard data:', error);
                message.error('Không thể tải dữ liệu tổng quan');
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, []);

    const stats = [
        {
            title: 'Tổng tài liệu',
            value: statsData?.totalDocuments || 0,
            icon: <FileTextOutlined />,
            color: '#1890ff'
        },
        {
            title: 'Lượt kiểm tra',
            value: statsData?.totalChecks || 0,
            icon: <CheckCircleOutlined />,
            color: '#52c41a'
        },
        {
            title: 'Tỷ lệ trùng khớp TB',
            value: `${statsData?.averageSimilarity.toFixed(1) || 0}%`,
            icon: <WarningOutlined />,
            color: '#fa8c16'
        },
        {
            title: 'Người dùng',
            value: statsData?.totalUsers || 0,
            icon: <TeamOutlined />,
            color: '#722ed1'
        },
    ];

    const columns = [
        {
            title: 'Tên tài liệu',
            dataIndex: 'sourceDocumentTitle',
            key: 'title',
            render: (text: string) => <Text strong ellipsis={{ tooltip: text }}>{text}</Text>
        },
        {
            title: 'Người nộp',
            dataIndex: 'userName',
            key: 'user'
        },
        {
            title: 'Ngày nộp',
            dataIndex: 'checkDate',
            key: 'date',
            render: (val: string) => new Date(val).toLocaleDateString('vi-VN')
        },
        {
            title: 'Tỷ lệ trùng',
            dataIndex: 'overallSimilarityPercentage',
            key: 'status',
            render: (val: number) => {
                let color = val > 30 ? 'red' : val > 15 ? 'orange' : 'green';
                return <Text style={{ color, fontWeight: 'bold' }}>{val.toFixed(1)}%</Text>
            }
        },
        {
            title: 'Trạng thái',
            dataIndex: 'overallSimilarityPercentage',
            key: 'result',
            render: (val: number) => {
                let status = val > 30 ? 'Nguy cơ' : val > 15 ? 'Xem xét' : 'An toàn';
                let color = status === 'Nguy cơ' ? 'volcano' : status === 'Xem xét' ? 'warning' : 'success';
                return <Tag color={color}>{status.toUpperCase()}</Tag>
            }
        },
    ];

    if (loading) {
        return (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '80vh' }}>
                <Spin size="large" tip="Đang tải dữ liệu..." />
            </div>
        );
    }

    return (
        <div className="animate-fade-in">
            <div style={{ marginBottom: 24 }}>
                <Title level={2}>Chào mừng trở lại, <span className="gradient-text">Quản trị viên BAU!</span></Title>
                <Text type="secondary">Đây là báo cáo tổng quan về tình hình liêm chính học thuật trong hệ thống.</Text>
            </div>

            <Row gutter={[24, 24]}>
                {stats.map((item, index) => (
                    <Col xs={24} sm={12} lg={6} key={index}>
                        <motion.div whileHover={{ y: -5 }}>
                            <Card className="glass-card" bordered={false}>
                                <Statistic
                                    title={<Text type="secondary">{item.title}</Text>}
                                    value={item.value}
                                    prefix={<div style={{
                                        color: item.color,
                                        marginRight: 10,
                                        fontSize: 24,
                                        background: `${item.color}10`,
                                        width: 45, height: 45, borderRadius: 10,
                                        display: 'flex', alignItems: 'center', justifyContent: 'center'
                                    }}>{item.icon}</div>}
                                    valueStyle={{ fontWeight: 700, marginTop: 10 }}
                                />
                            </Card>
                        </motion.div>
                    </Col>
                ))}
            </Row>

            <Row gutter={[24, 24]} style={{ marginTop: 24 }}>
                <Col span={24}>
                    <Card className="glass-card" title="Hoạt động kiểm tra gần đây" bordered={false}>
                        <Table
                            dataSource={recentActivity}
                            columns={columns}
                            pagination={false}
                            rowKey="id"
                            style={{ background: 'transparent' }}
                        />
                    </Card>
                </Col>
            </Row>
        </div>
    );
};

export default HomePage;
