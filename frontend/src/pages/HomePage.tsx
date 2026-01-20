import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Row, Col, Card, Statistic, Typography, Table, Tag, message, Spin, Button } from 'antd';
import {
    FileTextOutlined,
    CheckCircleOutlined,
    WarningOutlined,
    TeamOutlined,
    DownloadOutlined
} from '@ant-design/icons';
import { motion } from 'framer-motion';
import { useSelector } from 'react-redux';
import { RootState } from '../store';
import plagiarismApi, { PlagiarismStatisticsDto } from '../api/plagiarismApi';
import documentApi from '../api/documentApi';

const { Title, Text } = Typography;

const HomePage: React.FC = () => {
    const { user } = useSelector((state: RootState) => state.auth);
    const navigate = useNavigate();
    const [statsData, setStatsData] = useState<PlagiarismStatisticsDto | null>(null);
    const [recentActivity, setRecentActivity] = useState<any[]>([]);
    const [highRiskChecks, setHighRiskChecks] = useState<any[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (user?.role === 'Student') {
            navigate('/check', { replace: true });
        }
    }, [user, navigate]);

    useEffect(() => {
        if (user?.role === 'Student') return; // Don't fetch if redirecting

        const fetchData = async () => {
            setLoading(true);
            try {
                const promises: any[] = [
                    plagiarismApi.getStatistics(),
                    plagiarismApi.getHistory({ limit: 5 })
                ];

                // Only fetch high-risk for lecturers/admins
                if (user?.role !== 'Student') {
                    promises.push(plagiarismApi.getHighRisk(50, 10));
                }

                const results = await Promise.all(promises);

                setStatsData(results[0]);
                setRecentActivity(results[1]);
                if (results[2]) {
                    setHighRiskChecks(results[2]);
                }
            } catch (error) {
                console.error('Error fetching dashboard data:', error);
                message.error('Không thể tải dữ liệu tổng quan');
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, []);

    const isStudent = user?.role === 'Student';

    const stats = [
        {
            title: isStudent ? 'Tài liệu của tôi' : 'Tổng tài liệu',
            value: statsData?.totalDocuments || 0,
            icon: <FileTextOutlined />,
            color: '#1890ff'
        },
        {
            title: isStudent ? 'Lượt kiểm tra của tôi' : 'Tổng lượt kiểm tra',
            value: statsData?.totalChecks || 0,
            icon: <CheckCircleOutlined />,
            color: '#52c41a'
        },
        {
            title: isStudent ? 'Tỷ lệ trùng khớp TB' : 'Tỷ lệ trùng khớp hệ thống',
            value: `${statsData?.averageSimilarity.toFixed(1) || 0}%`,
            icon: <WarningOutlined />,
            color: '#fa8c16'
        },
        // Hide user count for students
        ...(isStudent ? [] : [{
            title: 'Người dùng hệ thống',
            value: statsData?.totalUsers || 0,
            icon: <TeamOutlined />,
            color: '#722ed1'
        }]),
    ];

    const columns = [
        {
            title: 'Tên tài liệu',
            dataIndex: 'sourceDocumentTitle',
            key: 'title',
            render: (text: string) => <Text strong ellipsis={{ tooltip: text }}>{text}</Text>
        },
        // Only show author if not student
        ...(!isStudent ? [{
            title: 'Người nộp',
            dataIndex: 'userName',
            key: 'user'
        }] : []),
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
                let color = val > 20 ? 'red' : val > 10 ? 'orange' : 'green';
                return <Text style={{ color, fontWeight: 'bold' }}>{val.toFixed(1)}%</Text>
            }
        },
        {
            title: 'Trạng thái',
            dataIndex: 'overallSimilarityPercentage',
            key: 'result',
            render: (val: number) => {
                let status = val > 20 ? 'Nguy cơ' : val > 10 ? 'Xem xét' : 'An toàn';
                let color = status === 'Nguy cơ' ? 'volcano' : status === 'Xem xét' ? 'warning' : 'success';
                return <Tag color={color}>{status.toUpperCase()}</Tag>
            }
        },
        {
            title: 'Thao tác',
            key: 'action',
            render: (_: any, record: any) => (
                <Button
                    type="text"
                    icon={<DownloadOutlined />}
                    onClick={() => window.open(documentApi.getDownloadUrl(record.sourceDocumentId), '_blank')}
                />
            )
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
                <Title level={2}>
                    {isStudent ? 'Chào bạn, ' : 'Chào mừng trở lại, '}
                    <span className="gradient-text">{user?.fullName}!</span>
                </Title>
                <Text type="secondary">
                    {isStudent
                        ? 'Dưới đây là tóm tắt các hoạt động nộp bài và kiểm tra đạo văn của bạn.'
                        : 'Đây là báo cáo tổng quan về tình hình liêm chính học thuật trong toàn hệ thống.'}
                </Text>
            </div>

            <Row gutter={[24, 24]}>
                {stats.map((item, index) => (
                    <Col xs={24} sm={12} lg={isStudent ? 8 : 6} key={index}>
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
                    <Card className="glass-card" title={isStudent ? "Lịch sử nộp bài gần đây" : "Hoạt động kiểm tra hệ thống"} bordered={false}>
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

            {/* Daily Check Limit Info for Students */}
            {user?.role === 'Student' && user?.remainingChecksToday !== undefined && (
                <Row gutter={[24, 24]} style={{ marginTop: 24 }}>
                    <Col span={24}>
                        <Card className="glass-card" bordered={false} style={{ background: user.remainingChecksToday <= 1 ? '#fff2e8' : '#f6ffed' }}>
                            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                                <div>
                                    <Text strong style={{ fontSize: 16 }}>Số lượt kiểm tra hôm nay</Text>
                                    <div style={{ marginTop: 8 }}>
                                        <Text style={{ fontSize: 24, fontWeight: 'bold', color: user.remainingChecksToday <= 1 ? '#fa8c16' : '#52c41a' }}>
                                            {user.remainingChecksToday}/{user.dailyCheckLimit}
                                        </Text>
                                        <Text type="secondary" style={{ marginLeft: 8 }}>lượt còn lại</Text>
                                    </div>
                                </div>
                                {user.remainingChecksToday === 0 && (
                                    <Tag color="error" style={{ fontSize: 14, padding: '4px 12px' }}>Đã hết lượt kiểm tra hôm nay</Tag>
                                )}
                                {user.remainingChecksToday === 1 && (
                                    <Tag color="warning" style={{ fontSize: 14, padding: '4px 12px' }}>Còn 1 lượt cuối cùng!</Tag>
                                )}
                            </div>
                        </Card>
                    </Col>
                </Row>
            )}

            {/* High Risk Checks Warning List (For Admins) */}
            {user?.role !== 'Student' && highRiskChecks.length > 0 && (
                <Row gutter={[24, 24]} style={{ marginTop: 24 }}>
                    <Col span={24}>
                        <Card
                            className="glass-card"
                            title={
                                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                    <WarningOutlined style={{ color: '#ff4d4f', fontSize: 20 }} />
                                    <Text strong style={{ fontSize: 16 }}>Cảnh báo nóng - Tỷ lệ đạo văn cao (≥50%)</Text>
                                </div>
                            }
                            bordered={false}
                            style={{ borderLeft: '4px solid #ff4d4f' }}
                        >
                            <Table
                                dataSource={highRiskChecks}
                                columns={[
                                    {
                                        title: 'Tên tài liệu',
                                        dataIndex: 'sourceDocumentTitle',
                                        key: 'title',
                                        render: (text: string) => <Text strong style={{ color: '#ff4d4f' }}>{text}</Text>
                                    },
                                    {
                                        title: 'Người nộp',
                                        dataIndex: 'userName',
                                        key: 'user'
                                    },
                                    {
                                        title: 'Ngày kiểm tra',
                                        dataIndex: 'checkDate',
                                        key: 'date',
                                        render: (val: string) => new Date(val).toLocaleDateString('vi-VN')
                                    },
                                    {
                                        title: 'Tỷ lệ trùng',
                                        dataIndex: 'overallSimilarityPercentage',
                                        key: 'similarity',
                                        render: (val: number) => (
                                            <Text style={{ color: '#ff4d4f', fontWeight: 'bold', fontSize: 16 }}>
                                                {val.toFixed(1)}%
                                            </Text>
                                        ),
                                        sorter: (a: any, b: any) => b.overallSimilarityPercentage - a.overallSimilarityPercentage
                                    },
                                    {
                                        title: 'Hành động',
                                        key: 'action',
                                        render: (_: any, record: any) => (
                                            <Button
                                                type="primary"
                                                danger
                                                onClick={() => navigate(`/results/${record.id}`)}
                                            >
                                                Xem chi tiết
                                            </Button>
                                        )
                                    }
                                ]}
                                pagination={false}
                                rowKey="id"
                            />
                        </Card>
                    </Col>
                </Row>
            )}
        </div>
    );
};

export default HomePage;
