import React, { useState, useEffect } from 'react';
import { Form, Input, Button, Card, Typography, message, Space } from 'antd';
import { LockOutlined, SafetyOutlined, KeyOutlined, ArrowLeftOutlined, MailOutlined, ReloadOutlined } from '@ant-design/icons';
import { motion } from 'framer-motion';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import axiosClient from '../api/axiosClient';

const { Title, Text } = Typography;

const ResetPasswordPage: React.FC = () => {
    const [form] = Form.useForm();
    const [loading, setLoading] = useState(false);
    const [resending, setResending] = useState(false);
    const [countdown, setCountdown] = useState(0);

    const navigate = useNavigate();
    const location = useLocation();
    const initialEmail = location.state?.email || '';

    useEffect(() => {
        let timer: any;
        if (countdown > 0) {
            timer = setTimeout(() => setCountdown(countdown - 1), 1000);
        }
        return () => clearTimeout(timer);
    }, [countdown]);

    const onFinish = async (values: any) => {
        setLoading(true);
        try {
            const { email, code, newPassword } = values;
            await axiosClient.post('/auth/reset-password', { email, code, newPassword });

            message.success('Mật khẩu đã được thay đổi thành công!');
            navigate('/login');
        } catch (error: any) {
            message.error(error || 'Mã xác nhận không đúng hoặc đã hết hạn');
        } finally {
            setLoading(false);
        }
    };

    const handleResendCode = async () => {
        const email = form.getFieldValue('email');
        if (!email) {
            message.warning('Vui lòng nhập email trước khi yêu cầu gửi lại mã!');
            return;
        }

        setResending(true);
        try {
            await axiosClient.post('/auth/forgot-password', { email });
            message.success('Mã xác nhận mới đã được gửi!');
            setCountdown(60); // 60 seconds cooldown
        } catch (error: any) {
            message.error(error.message || 'Không thể gửi lại mã, vui lòng thử lại sau');
        } finally {
            setResending(false);
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
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ duration: 0.5 }}
            >
                <Card className="glass-card" style={{ width: 450, border: 'none', borderRadius: 16 }}>
                    <div style={{ textAlign: 'center', marginBottom: 20 }}>
                        <Title level={2} style={{ margin: 0, color: '#003a8c' }}>ĐẶT LẠI MẬT KHẨU</Title>
                        <Text type="secondary">Vui lòng kiểm tra email và nhập mã xác nhận</Text>
                    </div>

                    <Form
                        form={form}
                        name="reset_password_form"
                        onFinish={onFinish}
                        size="large"
                        layout="vertical"
                        initialValues={{ email: initialEmail }}
                    >
                        <Form.Item
                            name="email"
                            label="Địa chỉ Email"
                            rules={[
                                { required: true, message: 'Vui lòng nhập email!' },
                                { type: 'email', message: 'Email không hợp lệ!' }
                            ]}
                        >
                            <Input prefix={<MailOutlined />} placeholder="example@email.com" />
                        </Form.Item>

                        <Form.Item
                            name="code"
                            label="Mã xác nhận"
                            rules={[{ required: true, message: 'Vui lòng nhập mã xác nhận 6 số!' }]}
                        >
                            <div style={{ display: 'flex', gap: '8px' }}>
                                <Input
                                    prefix={<KeyOutlined />}
                                    placeholder="6 chữ số"
                                    maxLength={6}
                                    style={{ letterSpacing: 2, fontWeight: 'bold' }}
                                />
                                <Button
                                    icon={<ReloadOutlined />}
                                    onClick={handleResendCode}
                                    disabled={countdown > 0}
                                    loading={resending}
                                    style={{ width: 140, fontSize: '14px' }}
                                >
                                    {countdown > 0 ? `${countdown}s` : 'Gửi lại mã'}
                                </Button>
                            </div>
                        </Form.Item>

                        <Form.Item
                            name="newPassword"
                            label="Mật khẩu mới"
                            rules={[
                                { required: true, message: 'Vui lòng nhập mật khẩu mới!' },
                                { min: 6, message: 'Mật khẩu phải từ 6 ký tự trở lên!' }
                            ]}
                        >
                            <Input.Password prefix={<LockOutlined />} placeholder="Nhập mật khẩu mới" />
                        </Form.Item>

                        <Form.Item
                            name="confirmPassword"
                            label="Xác nhận mật khẩu mới"
                            dependencies={['newPassword']}
                            rules={[
                                { required: true, message: 'Vui lòng nhập lại mật khẩu mới!' },
                                ({ getFieldValue }) => ({
                                    validator(_, value) {
                                        if (!value || getFieldValue('newPassword') === value) {
                                            return Promise.resolve();
                                        }
                                        return Promise.reject(new Error('Mật khẩu xác nhận không khớp!'));
                                    },
                                }),
                            ]}
                        >
                            <Input.Password prefix={<SafetyOutlined />} placeholder="Nhập lại mật khẩu mới" />
                        </Form.Item>

                        <Form.Item style={{ marginTop: 10, marginBottom: 15 }}>
                            <Button type="primary" htmlType="submit" block loading={loading} style={{ height: 48, borderRadius: 8, fontWeight: 'bold' }}>
                                XÁC NHẬN ĐỔI MẬT KHẨU
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

export default ResetPasswordPage;
