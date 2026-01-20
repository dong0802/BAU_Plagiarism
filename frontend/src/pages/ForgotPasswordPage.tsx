import React, { useState } from 'react';
import { Form, Input, Button, Card, Typography, message, Space } from 'antd';
import { MailOutlined, ArrowLeftOutlined } from '@ant-design/icons';
import { motion } from 'framer-motion';
import { useNavigate, Link } from 'react-router-dom';
import axiosClient from '../api/axiosClient';

const { Title, Text } = Typography;

const ForgotPasswordPage: React.FC = () => {
    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();

    const onFinish = async (values: any) => {
        setLoading(true);
        try {
            await axiosClient.post('/auth/forgot-password', values);
            message.success('Mã xác nhận đã được gửi về email của bạn!');
            navigate('/reset-password', { state: { email: values.email } });
        } catch (error: any) {
            message.error(error || 'Không thể gửi yêu cầu khôi phục mật khẩu');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={{
            height: '100vh',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: 'linear-gradient(135deg, #001529 0%, #003a8c 100%)',
            overflow: 'hidden',
        }}>
            <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.8 }}
            >
                <Card className="glass-card" style={{ width: 400, border: 'none' }}>
                    <div style={{ textAlign: 'center', marginBottom: 30 }}>
                        <Title level={3} style={{ margin: 0 }}>QUÊN MẬT KHẨU</Title>
                        <Text type="secondary">Nhập email để nhận mã xác nhận</Text>
                    </div>

                    <Form
                        name="forgot_password_form"
                        onFinish={onFinish}
                        size="large"
                        layout="vertical"
                    >
                        <Form.Item
                            name="email"
                            label="Email"
                            rules={[
                                { required: true, message: 'Vui lòng nhập email!' },
                                { type: 'email', message: 'Email không hợp lệ!' }
                            ]}
                        >
                            <Input prefix={<MailOutlined />} placeholder="example@email.com" />
                        </Form.Item>

                        <Form.Item>
                            <Button type="primary" htmlType="submit" block loading={loading}>
                                GỬI MÃ XÁC NHẬN
                            </Button>
                        </Form.Item>

                        <div style={{ textAlign: 'center' }}>
                            <Link to="/login">
                                <Space>
                                    <ArrowLeftOutlined /> Quay lại đăng nhập
                                </Space>
                            </Link>
                        </div>
                    </Form>
                </Card>
            </motion.div>
        </div>
    );
};

export default ForgotPasswordPage;
