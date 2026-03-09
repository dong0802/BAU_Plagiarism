import React, { useState, useEffect } from 'react';
import {
    SearchOutlined, DownloadOutlined, EyeOutlined, DeleteOutlined,
    FilePdfOutlined, UserOutlined, CalendarOutlined,
    UploadOutlined, InboxOutlined, FileSearchOutlined,
    CheckCircleOutlined, PlusCircleOutlined, InfoCircleOutlined,
    FileDoneOutlined, TeamOutlined, FolderOpenOutlined, FileTextOutlined
} from '@ant-design/icons';
import {
    Table, Card, Typography, Input, Space, Button, Tag,
    Tooltip, message, Modal, Row, Col, Statistic,
    Upload, Form, Select, Empty, Spin
} from 'antd';
import { useNavigate } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { RootState } from '../store';
import documentApi, { DocumentDto } from '../api/documentApi';
import catalogApi, { FacultyDto, SubjectDto } from '../api/catalogApi';

const { Title, Text, Paragraph } = Typography;
const { Dragger } = Upload;
const { Option } = Select;

const DocumentListPage: React.FC = () => {
    const { user } = useSelector((state: RootState) => state.auth);
    const navigate = useNavigate();
    const [documents, setDocuments] = useState<DocumentDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [searchText, setSearchText] = useState('');

    const isStudent = user?.role === 'Student';

    // Detail Modal states
    const [detailVisible, setDetailVisible] = useState(false);
    const [selectedDoc, setSelectedDoc] = useState<DocumentDto | null>(null);
    const [content, setContent] = useState<string>('');
    const [contentLoading, setContentLoading] = useState(false);

    // Upload states
    const [uploadVisible, setUploadVisible] = useState(false);
    const [uploadingFiles, setUploadingFiles] = useState(false);
    const [fileList, setFileList] = useState<any[]>([]);

    // Catalog states
    const [faculties, setFaculties] = useState<FacultyDto[]>([]);
    const [filteredSubjects, setFilteredSubjects] = useState<SubjectDto[]>([]);
    const [selectedFaculty, setSelectedFaculty] = useState<number | null>(null);
    const [selectedSubject, setSelectedSubject] = useState<number | null>(null);
    const [uploadForm] = Form.useForm();

    const fetchDocuments = async () => {
        setLoading(true);
        try {
            const data = await documentApi.getAll();
            setDocuments(data);
        } catch (error) {
            message.error('Không thể tải danh sách tài liệu');
        } finally {
            setLoading(false);
        }
    };

    const fetchCatalog = async () => {
        try {
            const facs = await catalogApi.getFaculties();
            setFaculties(facs);
        } catch (error) {
            console.error('Lỗi khi tải danh mục:', error);
        }
    };

    useEffect(() => {
        fetchDocuments();
        fetchCatalog();
    }, []);

    const handleBatchUpload = async () => {
        try {
            const values = await uploadForm.validateFields();

            if (fileList.length === 0) {
                message.warning('Vui lòng chọn ít nhất một file');
                return;
            }

            setUploadingFiles(true);
            let successCount = 0;
            let failCount = 0;

            for (const fileItem of fileList) {
                try {
                    const file = fileItem.originFileObj || fileItem;
                    await documentApi.upload({
                        file: file,
                        title: file.name.replace(/\.[^/.]+$/, ""),
                        documentType: 'Essay',
                        isPublic: values.isPublic ?? (isStudent ? false : true),
                        subjectId: values.subjectId
                    });
                    successCount++;
                } catch (error: any) {
                    failCount++;
                }
            }

            if (successCount > 0) {
                message.success(`Đã tải lên thành công ${successCount} tài liệu`);
                setFileList([]);
                uploadForm.resetFields();
                setUploadVisible(false);
                await fetchDocuments();
            }
            if (failCount > 0) {
                message.error(`Có ${failCount} tài liệu tải lên thất bại.`);
            }
        } catch (error) {
            console.error('Validation error:', error);
        } finally {
            setUploadingFiles(false);
        }
    };

    const handleDelete = async (id: number) => {
        Modal.confirm({
            title: 'Xác nhận xóa tài liệu?',
            content: 'Tài liệu sẽ bị xóa khỏi hệ thống và không thể khôi phục.',
            okText: 'Xóa',
            okType: 'danger',
            cancelText: 'Hủy',
            onOk: async () => {
                try {
                    await documentApi.delete(id);
                    message.success('Đã xóa tài liệu');
                    fetchDocuments();
                } catch (error) {
                    message.error('Xóa tài liệu thất bại');
                }
            }
        });
    };

    const handleTogglePublic = async (record: DocumentDto) => {
        try {
            const newStatus = !record.isPublic;
            await documentApi.update(record.id, {
                ...record,
                isPublic: newStatus,
                isActive: true
            });
            message.success(newStatus ? 'Đã duyệt vào kho đối soát' : 'Đã gỡ khỏi kho đối soát');
            fetchDocuments();
        } catch (error) {
            message.error('Thao tác thất bại');
        }
    };

    const handleDownload = (id: number) => {
        const url = documentApi.getDownloadUrl(id);
        window.open(url, '_blank');
    };

    const handleViewDetail = async (doc: DocumentDto) => {
        setSelectedDoc(doc);
        setDetailVisible(true);
        setContentLoading(true);
        try {
            const data = await documentApi.getContent(doc.id);
            setContent(data.content);
        } catch (error) {
            message.error('Không thể tải nội dung');
            setContent('Lỗi tải nội dung...');
        } finally {
            setContentLoading(false);
        }
    };

    const columns = [
        {
            title: 'Tài liệu',
            dataIndex: 'title',
            key: 'title',
            width: '35%',
            render: (text: string, record: DocumentDto) => (
                <Space size="middle">
                    <div className="file-icon-wrapper" style={{
                        background: record.isPublic ? 'rgba(82, 196, 26, 0.1)' : 'rgba(24, 144, 255, 0.1)',
                        padding: '10px',
                        borderRadius: '10px'
                    }}>
                        <FilePdfOutlined style={{ fontSize: 24, color: record.isPublic ? '#52c41a' : '#1890ff' }} />
                    </div>
                    <div>
                        <Text strong style={{ fontSize: '15px', color: '#1a3353' }}>{text}</Text>
                        <br />
                        <Text type="secondary" style={{ fontSize: '12px' }}>
                            {record.documentType || 'Bài tập'} • {(record.fileSize / 1024).toFixed(1)} KB
                        </Text>
                    </div>
                </Space>
            )
        },
        {
            title: 'Môn học & Khoa',
            key: 'catalog',
            width: '25%',
            render: (_: any, record: DocumentDto) => (
                <div>
                    <Text strong style={{ display: 'block' }}>{record.subjectName || 'N/A'}</Text>
                    <Text type="secondary" style={{ fontSize: '12px' }}>{record.facultyName || 'N/A'}</Text>
                </div>
            )
        },
        {
            title: 'Ngày & Tác giả',
            key: 'uploadInfo',
            width: '20%',
            render: (_: any, record: DocumentDto) => (
                <div>
                    <Space size={4} style={{ display: 'block' }}>
                        <CalendarOutlined style={{ fontSize: 12 }} />
                        <Text style={{ fontSize: '13px' }}>{new Date(record.uploadDate).toLocaleDateString('vi-VN')}</Text>
                    </Space>
                    {!isStudent && (
                        <Space size={4}>
                            <UserOutlined style={{ fontSize: 12 }} />
                            <Text type="secondary" style={{ fontSize: '12px' }}>{record.userName}</Text>
                        </Space>
                    )}
                </div>
            )
        },
        {
            title: 'Trạng thái',
            dataIndex: 'isPublic',
            key: 'isPublic',
            width: '10%',
            render: (isPublic: boolean) => (
                <Tag color={isPublic ? 'green' : 'orange'} style={{ borderRadius: '12px', border: 'none', padding: '2px 10px' }}>
                    {isPublic ? 'Đã duyệt' : 'Chờ duyệt'}
                </Tag>
            )
        },
        {
            title: 'Thao tác',
            key: 'action',
            width: '10%',
            align: 'right' as const,
            render: (_: any, record: DocumentDto) => (
                <Space size="small">
                    <Tooltip title="Đối soát">
                        <Button
                            type="text"
                            className="action-btn-hover"
                            icon={<FileSearchOutlined style={{ color: '#722ed1' }} />}
                            onClick={() => navigate('/check', { state: { sourceDocId: record.id, fileName: record.title } })}
                        />
                    </Tooltip>
                    <Tooltip title="Chi tiết">
                        <Button type="text" className="action-btn-hover" icon={<EyeOutlined />} onClick={() => handleViewDetail(record)} />
                    </Tooltip>
                    <Tooltip title="Tải về">
                        <Button type="text" className="action-btn-hover" icon={<DownloadOutlined />} onClick={() => handleDownload(record.id)} />
                    </Tooltip>
                    {!record.isPublic && (
                        <Tooltip title="Xóa">
                            <Button type="text" danger className="action-btn-hover" icon={<DeleteOutlined />} onClick={() => handleDelete(record.id)} />
                        </Tooltip>
                    )}
                    {!isStudent && (
                        <Tooltip title={record.isPublic ? "Gỡ khỏi kho" : "Duyệt vào kho"}>
                            <Button
                                type="text"
                                className="action-btn-hover"
                                icon={record.isPublic ? <CheckCircleOutlined style={{ color: '#52c41a' }} /> : <PlusCircleOutlined style={{ color: '#faad14' }} />}
                                onClick={() => handleTogglePublic(record)}
                            />
                        </Tooltip>
                    )}
                </Space>
            ),
        }
    ];

    const filteredDocs = documents.filter(d => {
        const matchSearch = d.title.toLowerCase().includes(searchText.toLowerCase()) ||
            (d.userName && d.userName.toLowerCase().includes(searchText.toLowerCase())) ||
            (d.subjectName && d.subjectName.toLowerCase().includes(searchText.toLowerCase())) ||
            (d.facultyName && d.facultyName.toLowerCase().includes(searchText.toLowerCase()));

        const matchFaculty = !selectedFaculty || d.facultyId === selectedFaculty;
        const matchSubject = !selectedSubject || d.subjectId === selectedSubject;

        return matchSearch && matchFaculty && matchSubject;
    });

    return (
        <div className="animate-fade-in" style={{ padding: '0 20px' }}>
            {/* Header Area */}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', marginBottom: 32 }}>
                <div>
                    <Title level={2} style={{ marginBottom: 4 }}>
                        {isStudent ? 'Kho tài liệu của tôi' : 'Kho tài liệu lưu trữ'}
                    </Title>
                    <Text type="secondary">
                        {isStudent ? 'Quản lý các tài liệu bạn đã tải lên hệ thống' : 'Quản lý toàn bộ cơ sở dữ liệu tài liệu của HVNH'}
                    </Text>
                </div>
                <Space size="middle">
                    <Input
                        placeholder="Tìm kiếm theo tên, môn, tác giả..."
                        prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
                        style={{ width: 320, height: 40 }}
                        onChange={e => setSearchText(e.target.value)}
                        allowClear
                    />
                    {(isStudent || user?.role === 'Admin') && (
                        <Button
                            type="primary"
                            icon={<UploadOutlined />}
                            size="large"
                            onClick={() => setUploadVisible(true)}
                            style={{ display: 'flex', alignItems: 'center' }}
                        >
                            Tải lên tài liệu
                        </Button>
                    )}
                </Space>
            </div>

            {/* Statistics Dashboard */}
            <Row gutter={[24, 24]} style={{ marginBottom: 32 }}>
                <Col xs={24} sm={8}>
                    <Card className="glass-card stat-card" bordered={false}>
                        <Statistic
                            title={<Space><FileDoneOutlined />Tổng số tài liệu</Space>}
                            value={documents.length}
                            valueStyle={{ color: 'var(--primary-color)', fontWeight: 700 }}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={8}>
                    <Card className="glass-card stat-card" bordered={false}>
                        <Statistic
                            title={<Space><FolderOpenOutlined />Đã duyệt vào kho</Space>}
                            value={documents.filter(d => d.isPublic).length}
                            valueStyle={{ color: '#52c41a', fontWeight: 700 }}
                            suffix={<Text type="secondary" style={{ fontSize: 14 }}>tập tin</Text>}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={8}>
                    <Card className="glass-card stat-card" bordered={false}>
                        <Statistic
                            title={<Space><TeamOutlined />Người đóng góp</Space>}
                            value={Array.from(new Set(documents.map(d => d.userId))).length}
                            valueStyle={{ color: '#722ed1', fontWeight: 700 }}
                        />
                    </Card>
                </Col>
            </Row>

            {/* Main Content with Sidebar Filter */}
            <Row gutter={24}>
                <Col xs={24} lg={6}>
                    <Card
                        className="glass-card"
                        bordered={false}
                        title={<Space><SearchOutlined /> Bộ lọc</Space>}
                        style={{ marginBottom: 24 }}
                    >
                        <Form layout="vertical">
                            <Form.Item label="Khoa quản lý">
                                <Select
                                    placeholder="Tất cả các khoa"
                                    allowClear
                                    onChange={(val) => {
                                        setSelectedFaculty(val);
                                        setSelectedSubject(null);
                                        if (val) {
                                            catalogApi.getSubjects(undefined, val).then(setFilteredSubjects);
                                        } else {
                                            setFilteredSubjects([]);
                                        }
                                    }}
                                    value={selectedFaculty}
                                >
                                    {faculties.map(f => (<Option key={f.id} value={f.id}>{f.name}</Option>))}
                                </Select>
                            </Form.Item>
                            <Form.Item label="Môn học">
                                <Select
                                    placeholder="Tất cả môn học"
                                    allowClear
                                    disabled={!selectedFaculty}
                                    onChange={setSelectedSubject}
                                    value={selectedSubject}
                                >
                                    {filteredSubjects.map(s => (<Option key={s.id} value={s.id}>{s.name}</Option>))}
                                </Select>
                            </Form.Item>
                            <Button
                                block
                                onClick={() => {
                                    setSelectedFaculty(null);
                                    setSelectedSubject(null);
                                    setFilteredSubjects([]);
                                }}
                                icon={<DeleteOutlined />}
                            >
                                Xóa bộ lọc
                            </Button>
                        </Form>
                    </Card>

                    <Card className="glass-card" style={{ background: 'var(--primary-gradient)', color: 'white', border: 'none' }}>
                        <Title level={5} style={{ color: 'white', margin: 0 }}>Gợi ý:</Title>
                        <Paragraph style={{ color: 'rgba(255,255,255,0.85)', fontSize: '13px', marginTop: 12 }}>
                            Sử dụng kho tài liệu để làm cơ sở đối soát tin cậy nhất cho các bài làm của sinh viên HVNH.
                        </Paragraph>
                    </Card>
                </Col>

                <Col xs={24} lg={18}>
                    <Card className="glass-card" bordered={false} bodyStyle={{ padding: 0 }}>
                        <Table
                            columns={columns}
                            dataSource={filteredDocs}
                            rowKey="id"
                            loading={loading}
                            className="premium-table"
                            pagination={{
                                pageSize: 10,
                                showSizeChanger: true,
                                showTotal: (total) => `Tổng cộng ${total} tài liệu`,
                                position: ['bottomRight']
                            }}
                            locale={{
                                emptyText: <Empty description="Không tìm thấy tài liệu nào trong kho" image={Empty.PRESENTED_IMAGE_SIMPLE} />
                            }}
                        />
                    </Card>
                </Col>
            </Row>

            {/* Upload Modal */}
            <Modal
                title={
                    <Space>
                        <UploadOutlined style={{ color: 'var(--primary-color)' }} />
                        <span>Tải lên tài liệu mới</span>
                    </Space>
                }
                open={uploadVisible}
                onCancel={() => !uploadingFiles && setUploadVisible(false)}
                width={600}
                centered
                footer={[
                    <Button key="cancel" onClick={() => setUploadVisible(false)} disabled={uploadingFiles}>
                        Hủy
                    </Button>,
                    <Button key="upload" type="primary" loading={uploadingFiles} onClick={handleBatchUpload}>
                        Bắt đầu tải lên ({fileList.length} files)
                    </Button>
                ]}
            >
                <div style={{ marginBottom: 24 }}>
                    <Dragger
                        multiple
                        fileList={fileList}
                        className="premium-dragger"
                        onRemove={(file) => {
                            setFileList(prev => prev.filter(f => f.uid !== file.uid));
                        }}
                        beforeUpload={(file) => {
                            setFileList(prev => [...prev, file]);
                            return false;
                        }}
                        style={{ background: '#fafafa', borderRadius: '12px', border: '2px dashed #d9d9d9' }}
                    >
                        <p className="ant-upload-drag-icon"><InboxOutlined style={{ color: 'var(--primary-color)' }} /></p>
                        <p className="ant-upload-text" style={{ fontWeight: 500 }}>Bấm hoặc kéo thả file vào vùng này</p>
                        <p className="ant-upload-hint">Hỗ trợ Word (.docx), PDF (.pdf), Text (.txt)</p>
                    </Dragger>
                </div>

                <Form form={uploadForm} layout="vertical" initialValues={{ isPublic: !isStudent }}>
                    <Row gutter={16}>
                        <Col span={12}>
                            <Form.Item name="facultyId" label="Khoa quản lý" rules={[{ required: true, message: 'Chọn khoa' }]}>
                                <Select
                                    placeholder="Chọn khoa..."
                                    onChange={async (val) => {
                                        setSelectedFaculty(val);
                                        uploadForm.setFieldsValue({ subjectId: undefined });
                                        const subs = await catalogApi.getSubjects(undefined, val);
                                        setFilteredSubjects(subs);
                                    }}
                                >
                                    {faculties.map(f => (<Option key={f.id} value={f.id}>{f.name}</Option>))}
                                </Select>
                            </Form.Item>
                        </Col>
                        <Col span={12}>
                            <Form.Item name="subjectId" label="Môn học" rules={[{ required: true, message: 'Chọn môn' }]}>
                                <Select placeholder="Chọn môn..." disabled={!selectedFaculty} showSearch optionFilterProp="children">
                                    {filteredSubjects.map(s => (<Option key={s.id} value={s.id}>{s.name}</Option>))}
                                </Select>
                            </Form.Item>
                        </Col>
                    </Row>

                    {!isStudent && (
                        <Form.Item name="isPublic" label="Quy trình xử lý">
                            <Select>
                                <Option value={true}>Duyệt vào kho đối soát ngay</Option>
                                <Option value={false}>Lưu dưới dạng bản nháp</Option>
                            </Select>
                        </Form.Item>
                    )}
                    <div style={{ background: '#f0faff', padding: '12px', borderRadius: '8px', marginBottom: '16px', display: 'flex', gap: '10px' }}>
                        <InfoCircleOutlined style={{ color: '#1890ff', marginTop: '3px' }} />
                        <Text type="secondary" style={{ fontSize: '13px' }}>
                            Các tài liệu sau khi tải lên sẽ được trích xuất văn bản và băm dữ liệu để phục vụ việc đối soát đạo văn.
                        </Text>
                    </div>
                </Form>
            </Modal>

            {/* Preview Modal */}
            <Modal
                title={
                    <div style={{ borderBottom: '1px solid #f0f0f0', paddingBottom: '16px', marginBottom: '16px' }}>
                        <Title level={4} style={{ margin: 0 }}>{selectedDoc?.title}</Title>
                        <Text type="secondary" style={{ fontSize: '13px' }}>
                            <FileTextOutlined /> Xem nhanh nội dung tài liệu
                        </Text>
                    </div>
                }
                open={detailVisible}
                onCancel={() => setDetailVisible(false)}
                width={800}
                centered
                className="premium-modal"
                footer={[
                    <Button key="download" icon={<DownloadOutlined />} onClick={() => handleDownload(selectedDoc!.id)}>
                        Tải file gốc
                    </Button>,
                    <Button key="close" type="primary" onClick={() => setDetailVisible(false)}>
                        Đóng lại
                    </Button>
                ]}
            >
                <div style={{ maxHeight: '60vh', overflowY: 'auto', padding: '0 8px' }} className="custom-scrollbar">
                    {contentLoading ? (
                        <div style={{ textAlign: 'center', padding: '50px' }}>
                            <Spin size="large" tip="Đang tải nội dung văn bản..." />
                        </div>
                    ) : (
                        <Paragraph style={{
                            whiteSpace: 'pre-wrap',
                            background: '#f9f9f9',
                            padding: '24px',
                            borderRadius: '12px',
                            lineHeight: 1.8,
                            fontSize: '15px'
                        }}>
                            {content || 'Không có nội dung văn bản.'}
                        </Paragraph>
                    )}
                </div>
            </Modal>
        </div>
    );
};

export default DocumentListPage;

