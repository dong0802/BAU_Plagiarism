import React, { useState } from 'react';
import { Form, Input, Button, Card, Typography, message, Checkbox } from 'antd';
import { UserOutlined, LockOutlined } from '@ant-design/icons';
import { motion } from 'framer-motion';
import { useDispatch } from 'react-redux';
import { setCredentials } from '../store/slices/authSlice';
import { useNavigate } from 'react-router-dom';
import axiosClient from '../api/axiosClient';

const { Title, Text } = Typography;

const LoginPage: React.FC = () => {
    const [loading, setLoading] = useState(false);
    const dispatch = useDispatch();
    const navigate = useNavigate();

    const onFinish = async (values: any) => {
        setLoading(true);
        try {
            const response = await axiosClient.post<any>('/auth/login', values) as any;

            // response is already response.data due to axiosClient interceptor
            dispatch(setCredentials(response));
            message.success('Đăng nhập thành công!');
            navigate('/');
        } catch (error: any) {
            console.error('Login error:', error);
            message.error(error.message || 'Sai tài khoản hoặc mật khẩu');
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
            position: 'relative'
        }}>
            {/* Background Decor */}
            <div style={{
                position: 'absolute',
                width: '500px',
                height: '500px',
                background: 'rgba(24, 144, 255, 0.1)',
                borderRadius: '50%',
                top: '-100px',
                right: '-100px',
                filter: 'blur(100px)'
            }} />
            <div style={{
                position: 'absolute',
                width: '400px',
                height: '400px',
                background: 'rgba(250, 140, 22, 0.05)',
                borderRadius: '50%',
                bottom: '-50px',
                left: '-50px',
                filter: 'blur(80px)'
            }} />

            <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.8 }}
            >
                <Card className="glass-card" style={{ width: 400, border: 'none' }}>
                    <div style={{ textAlign: 'center', marginBottom: 30 }}>
                        <div style={{
                            width: 60, height: 60, background: '#003a8c', borderRadius: 12,
                            display: 'flex', alignItems: 'center', justifyContent: 'center',
                            margin: '0 auto 15px', color: 'white', fontSize: 24, fontWeight: 'bold'
                        }}>
                            BAU
                        </div>
                        <Title level={3} style={{ margin: 0 }}>HỆ THỐNG ĐẠO VĂN</Title>
                        <Text type="secondary">Học viện Ngân hàng</Text>
                    </div>

                    <Form
                        name="login_form"
                        initialValues={{ remember: true }}
                        onFinish={onFinish}
                        size="large"
                        layout="vertical"
                    >
                        <Form.Item
                            name="username"
                            rules={[{ required: true, message: 'Vui lòng nhập tên đăng nhập!' }]}
                        >
                            <Input prefix={<UserOutlined />} placeholder="Tên đăng nhập" />
                        </Form.Item>

                        <Form.Item
                            name="password"
                            rules={[{ required: true, message: 'Vui lòng nhập mật khẩu!' }]}
                        >
                            <Input.Password prefix={<LockOutlined />} placeholder="Mật khẩu" />
                        </Form.Item>

                        <div style={{ marginBottom: 20, display: 'flex', justifyContent: 'space-between' }}>
                            <Checkbox>Ghi nhớ</Checkbox>
                            <a href="#">Quên mật khẩu?</a>
                        </div>

                        <Form.Item>
                            <Button type="primary" htmlType="submit" block loading={loading}>
                                ĐĂNG NHẬP
                            </Button>
                        </Form.Item>

                        <div style={{ textAlign: 'center' }}>
                            <Text type="secondary">Chưa có tài khoản? <a href="#">Đăng ký ngay</a></Text>
                        </div>
                    </Form>
                </Card>
            </motion.div>
        </div>
    );
};

export default LoginPage;
