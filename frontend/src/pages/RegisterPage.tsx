import React, { useState } from 'react';
import { Form, Input, Button, Card, Typography, message, Space, Select } from 'antd';
import { UserOutlined, LockOutlined, MailOutlined, ArrowLeftOutlined, IdcardOutlined } from '@ant-design/icons';
import { motion } from 'framer-motion';
import { useNavigate, Link } from 'react-router-dom';
import axiosClient from '../api/axiosClient';

const { Title, Text } = Typography;
const { Option } = Select;

const RegisterPage: React.FC = () => {
    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();

    const onFinish = async (values: any) => {
        setLoading(true);
        try {
            await axiosClient.post('/auth/register', values);
            message.success('Đăng ký tài khoản thành công! Vui lòng đăng nhập.');
            navigate('/login');
        } catch (error: any) {
            message.error(error || 'Đăng ký không thành công. Tên đăng nhập hoặc email có thể đã tồn tại.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={{
            minHeight: '100vh',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: 'linear-gradient(135deg, #001529 0%, #003a8c 100%)',
            padding: '40px 20px',
        }}>
            <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.8 }}
            >
                <Card className="glass-card" style={{ width: 450, border: 'none', borderRadius: 16 }}>
                    <div style={{ textAlign: 'center', marginBottom: 24 }}>
                        <div style={{
                            width: 60, height: 60, background: '#003a8c', borderRadius: 12,
                            display: 'flex', alignItems: 'center', justifyContent: 'center',
                            margin: '0 auto 15px', color: 'white', fontSize: 24, fontWeight: 'bold'
                        }}>
                            BAU
                        </div>
                        <Title level={2} style={{ margin: 0 }}>ĐĂNG KÝ TÀI KHOẢN</Title>
                        <Text type="secondary">Tham gia hệ thống kiểm tra đạo văn BAU</Text>
                    </div>

                    <Form
                        name="register_form"
                        onFinish={onFinish}
                        size="large"
                        layout="vertical"
                        initialValues={{ role: 'Student' }}
                    >
                        <Form.Item
                            name="fullName"
                            label="Họ và tên"
                            rules={[{ required: true, message: 'Vui lòng nhập họ tên của bạn!' }]}
                        >
                            <Input prefix={<UserOutlined />} placeholder="Nguyễn Văn A" />
                        </Form.Item>

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
                            name="username"
                            label="Tên đăng nhập"
                            rules={[{ required: true, message: 'Vui lòng nhập tên đăng nhập!' }]}
                        >
                            <Input prefix={<IdcardOutlined />} placeholder="username123" />
                        </Form.Item>

                        <Form.Item
                            name="password"
                            label="Mật khẩu"
                            rules={[
                                { required: true, message: 'Vui lòng nhập mật khẩu!' },
                                { min: 6, message: 'Mật khẩu phải từ 6 ký tự!' }
                            ]}
                        >
                            <Input.Password prefix={<LockOutlined />} placeholder="Mật khẩu" />
                        </Form.Item>

                        <Form.Item
                            name="role"
                            label="Bạn là?"
                            rules={[{ required: true }]}
                        >
                            <Select placeholder="Chọn vai trò">
                                <Option value="Student">Sinh viên</Option>
                                <Option value="Admin">Giảng viên / Quản trị viên</Option>
                            </Select>
                        </Form.Item>

                        <Form.Item>
                            <Button type="primary" htmlType="submit" block loading={loading} style={{ height: 48, borderRadius: 8, fontWeight: 'bold' }}>
                                ĐĂNG KÝ NGAY
                            </Button>
                        </Form.Item>

                        <div style={{ textAlign: 'center' }}>
                            <Link to="/login">
                                <Space>
                                    <ArrowLeftOutlined /> Đã có tài khoản? Đăng nhập
                                </Space>
                            </Link>
                        </div>
                    </Form>
                </Card>
            </motion.div>
        </div>
    );
};

export default RegisterPage;
