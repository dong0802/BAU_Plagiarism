import React, { useState, useEffect } from 'react';
import {
    Table, Tag, Space, Button, Card, Typography,
    Input, Modal, Form, Select, message, Popconfirm
} from 'antd';
import {
    UserAddOutlined,
    EditOutlined,
    DeleteOutlined,
    SearchOutlined,
    KeyOutlined
} from '@ant-design/icons';
import axiosClient from '../api/axiosClient';

const { Title } = Typography;
const { Option } = Select;

interface User {
    id: number;
    username: string;
    fullName: string;
    email: string;
    role: string;
    studentId?: string;
    lecturerId?: string;
    isActive: boolean;
    createdDate: string;
}

const UserManagementPage: React.FC = () => {
    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(false);
    const [isModalVisible, setIsModalVisible] = useState(false);
    const [editingUser, setEditingUser] = useState<User | null>(null);
    const [form] = Form.useForm();
    const [searchText, setSearchText] = useState('');
    const [isResetPwdVisible, setIsResetPwdVisible] = useState(false);
    const [resettingUser, setResettingUser] = useState<User | null>(null);
    const [resetForm] = Form.useForm();

    const fetchUsers = async () => {
        setLoading(true);
        try {
            // Trong thực tế sẽ gọi API
            const response = await axiosClient.get<User[]>('/users');
            setUsers(response as unknown as User[]);
        } catch (error) {
            console.error('Failed to fetch users:', error);
            // Mock data for demo if API fails
            const mockUsers: User[] = [
                { id: 1, username: 'admin', fullName: 'Quản trị viên', email: 'admin@bau.edu.vn', role: 'Admin', isActive: true, createdDate: '2024-01-01' },
                { id: 2, username: 'gv001', fullName: 'TS. Nguyễn Văn An', email: 'nvan@bau.edu.vn', role: 'Lecturer', lecturerId: 'GV001', isActive: true, createdDate: '2024-01-10' },
                { id: 3, username: '21a4010001', fullName: 'Nguyễn Văn A', email: '21a4010001@sv.bau.edu.vn', role: 'Student', studentId: '21A4010001', isActive: true, createdDate: '2024-01-15' },
            ];
            setUsers(mockUsers);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchUsers();
    }, []);

    const showModal = (user?: User) => {
        if (user) {
            setEditingUser(user);
            form.setFieldsValue(user);
        } else {
            setEditingUser(null);
            form.resetFields();
        }
        setIsModalVisible(true);
    };

    const handleCancel = () => {
        setIsModalVisible(false);
    };

    const onFinish = async (values: any) => {
        try {
            if (editingUser) {
                await axiosClient.put(`/users/${editingUser.id}`, values);
                message.success('Cập nhật người dùng thành công');
            } else {
                await axiosClient.post('/users', values);
                message.success('Thêm người dùng mới thành công');
            }
            setIsModalVisible(false);
            fetchUsers();
        } catch (error) {
            message.error('Thao tác thất bại');
        }
    };

    const handleDelete = async (id: number) => {
        try {
            await axiosClient.delete(`/users/${id}`);
            message.success('Đã xóa người dùng');
            fetchUsers();
        } catch (error) {
            message.error('Xóa thất bại');
        }
    };

    const columns = [
        {
            title: 'Tài khoản',
            dataIndex: 'username',
            key: 'username',
            render: (text: string) => <span style={{ fontWeight: 600 }}>{text}</span>,
        },
        {
            title: 'Họ tên',
            dataIndex: 'fullName',
            key: 'fullName',
        },
        {
            title: 'Email',
            dataIndex: 'email',
            key: 'email',
        },
        {
            title: 'Vai trò',
            dataIndex: 'role',
            key: 'role',
            render: (role: string) => {
                let color = 'blue';
                if (role === 'Admin') color = 'gold';
                if (role === 'Lecturer') color = 'cyan';
                return <Tag color={color}>{role.toUpperCase()}</Tag>;
            },
        },
        {
            title: 'Trạng thái',
            dataIndex: 'isActive',
            key: 'isActive',
            render: (active: boolean) => (
                <Tag color={active ? 'green' : 'red'}>
                    {active ? 'ĐANG HOẠT ĐỘNG' : 'BỊ KHÓA'}
                </Tag>
            ),
        },
        {
            title: 'Thao tác',
            key: 'action',
            render: (_: any, record: User) => (
                <Space size="middle">
                    <Button
                        type="text"
                        icon={<EditOutlined style={{ color: '#1890ff' }} />}
                        onClick={() => showModal(record)}
                    />
                    <Button
                        type="text"
                        icon={<KeyOutlined style={{ color: '#faad14' }} />}
                        onClick={() => {
                            setResettingUser(record);
                            setIsResetPwdVisible(true);
                        }}
                    />
                    <Popconfirm
                        title="Bạn có chắc chắn muốn xóa người dùng này?"
                        onConfirm={() => handleDelete(record.id)}
                        okText="Có"
                        cancelText="Không"
                    >
                        <Button type="text" icon={<DeleteOutlined style={{ color: '#ff4d4f' }} />} />
                    </Popconfirm>
                </Space>
            ),
        },
    ];

    const filteredUsers = users.filter(u =>
        u.fullName.toLowerCase().includes(searchText.toLowerCase()) ||
        u.username.toLowerCase().includes(searchText.toLowerCase())
    );

    return (
        <div className="animate-fade-in">
            <Card className="glass-card" bordered={false}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
                    <div>
                        <Title level={3} style={{ margin: 0 }}>Quản lý người dùng</Title>
                        <Typography.Text type="secondary">Quản trị viên có thể thêm, sửa, xóa và phân quyền người dùng</Typography.Text>
                    </div>
                    <Button
                        type="primary"
                        icon={<UserAddOutlined />}
                        size="large"
                        onClick={() => showModal()}
                        className="gradient-btn"
                    >
                        Thêm người dùng
                    </Button>
                </div>

                <div style={{ marginBottom: 16 }}>
                    <Input
                        placeholder="Tìm kiếm theo tên hoặc tài khoản..."
                        prefix={<SearchOutlined />}
                        onChange={e => setSearchText(e.target.value)}
                        style={{ width: 300 }}
                    />
                </div>

                <Table
                    columns={columns}
                    dataSource={filteredUsers}
                    loading={loading}
                    rowKey="id"
                    pagination={{ pageSize: 10 }}
                />
            </Card>

            <Modal
                title={editingUser ? "Cập nhật người dùng" : "Thêm người dùng mới"}
                open={isModalVisible}
                onCancel={handleCancel}
                footer={null}
                width={600}
            >
                <Form
                    form={form}
                    layout="vertical"
                    onFinish={onFinish}
                >
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                        <Form.Item
                            name="username"
                            label="Tài khoản"
                            rules={[{ required: true, message: 'Vui lòng nhập tài khoản' }]}
                        >
                            <Input disabled={!!editingUser} />
                        </Form.Item>

                        {!editingUser && (
                            <Form.Item
                                name="password"
                                label="Mật khẩu"
                                rules={[{ required: true, message: 'Vui lòng nhập mật khẩu' }]}
                            >
                                <Input.Password />
                            </Form.Item>
                        )}

                        <Form.Item
                            name="fullName"
                            label="Họ tên"
                            rules={[{ required: true, message: 'Vui lòng nhập họ tên' }]}
                        >
                            <Input />
                        </Form.Item>

                        <Form.Item
                            name="email"
                            label="Email"
                            rules={[
                                { required: true, message: 'Vui lòng nhập email' },
                                { type: 'email', message: 'Email không hợp lệ' }
                            ]}
                        >
                            <Input />
                        </Form.Item>

                        <Form.Item
                            name="role"
                            label="Vai trò"
                            rules={[{ required: true, message: 'Vui lòng chọn vai trò' }]}
                        >
                            <Select>
                                <Option value="Admin">Quản trị viên</Option>
                                <Option value="Lecturer">Giảng viên</Option>
                                <Option value="Student">Sinh viên</Option>
                            </Select>
                        </Form.Item>

                        <Form.Item
                            name="isActive"
                            label="Trạng thái"
                            initialValue={true}
                        >
                            <Select>
                                <Option value={true}>Hoạt động</Option>
                                <Option value={false}>Khóa</Option>
                            </Select>
                        </Form.Item>

                        <Form.Item noStyle shouldUpdate={(prevValues, currentValues) => prevValues.role !== currentValues.role}>
                            {({ getFieldValue }) => {
                                const role = getFieldValue('role');
                                if (role === 'Student') {
                                    return (
                                        <Form.Item
                                            name="studentId"
                                            label="Mã sinh viên"
                                            rules={[{ required: true, message: 'Vui lòng nhập mã sinh viên' }]}
                                        >
                                            <Input />
                                        </Form.Item>
                                    );
                                }
                                if (role === 'Lecturer') {
                                    return (
                                        <Form.Item
                                            name="lecturerId"
                                            label="Mã giảng viên"
                                            rules={[{ required: true, message: 'Vui lòng nhập mã giảng viên' }]}
                                        >
                                            <Input />
                                        </Form.Item>
                                    );
                                }
                                return null;
                            }}
                        </Form.Item>
                    </div>

                    <Form.Item style={{ marginTop: 24, textAlign: 'right', marginBottom: 0 }}>
                        <Space>
                            <Button onClick={handleCancel}>Hủy</Button>
                            <Button type="primary" htmlType="submit">
                                {editingUser ? "Cập nhật" : "Tạo mới"}
                            </Button>
                        </Space>
                    </Form.Item>
                </Form>
            </Modal>

            <Modal
                title={`Đặt lại mật khẩu cho ${resettingUser?.username}`}
                open={isResetPwdVisible}
                onCancel={() => setIsResetPwdVisible(false)}
                footer={null}
                width={400}
            >
                <Form
                    form={resetForm}
                    layout="vertical"
                    onFinish={async (values) => {
                        try {
                            // Backend ResetPassword expects a string (the password) as body
                            await axiosClient.post(`/users/${resettingUser?.id}/reset-password`, `"${values.newPassword}"`, {
                                headers: { 'Content-Type': 'application/json' }
                            });
                            message.success('Đặt lại mật khẩu thành công');
                            setIsResetPwdVisible(false);
                            resetForm.resetFields();
                        } catch (error) {
                            message.error('Đặt lại mật khẩu thất bại');
                        }
                    }}
                >
                    <Form.Item
                        name="newPassword"
                        label="Mật khẩu mới"
                        rules={[{ required: true, message: 'Vui lòng nhập mật khẩu mới' }, { min: 6, message: 'Mật khẩu phải ít nhất 6 ký tự' }]}
                    >
                        <Input.Password />
                    </Form.Item>
                    <Form.Item style={{ textAlign: 'right', marginBottom: 0 }}>
                        <Space>
                            <Button onClick={() => setIsResetPwdVisible(false)}>Hủy</Button>
                            <Button type="primary" htmlType="submit">Xác nhận</Button>
                        </Space>
                    </Form.Item>
                </Form>
            </Modal>
        </div>
    );
};

export default UserManagementPage;
