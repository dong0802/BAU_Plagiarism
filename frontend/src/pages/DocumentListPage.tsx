import React, { useState, useEffect } from 'react';
import { Table, Card, Typography, Input, Space, Button, Tag, Tooltip, message, Modal, Divider, Spin, Upload } from 'antd';
import { SearchOutlined, DownloadOutlined, EyeOutlined, DeleteOutlined, FilePdfOutlined, UserOutlined, CalendarOutlined, FileTextOutlined, UploadOutlined, InboxOutlined, FileSearchOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { RootState } from '../store';
import documentApi, { DocumentDto } from '../api/documentApi';

const { Title, Text, Paragraph } = Typography;
const { Dragger } = Upload;

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

    const handleBatchUpload = async () => {
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
                    isPublic: false
                });
                successCount++;
            } catch (error) {
                failCount++;
            }
        }

        setUploadingFiles(false);
        if (successCount > 0) {
            message.success(`Đã tải lên thành công ${successCount} tài liệu`);
            setFileList([]);
            setUploadVisible(false);
            fetchDocuments();
        }
        if (failCount > 0) {
            message.error(`Thất bại ${failCount} tài liệu`);
        }
    };

    useEffect(() => {
        fetchDocuments();
    }, []);

    const handleDelete = async (id: number) => {
        try {
            await documentApi.delete(id);
            message.success('Đã xóa tài liệu');
            fetchDocuments();
        } catch (error) {
            message.error('Xóa tài liệu thất bại');
        }
    };

    const handleDownload = (id: number) => {
        const url = documentApi.getDownloadUrl(id);
        window.open(url, '_blank');
    };

    const handlePrint = () => {
        window.print();
    };


    const handleViewDetail = async (doc: DocumentDto) => {
        setSelectedDoc(doc);
        setDetailVisible(true);
        setContentLoading(true);
        try {
            const data = await documentApi.getContent(doc.id);
            setContent(data.content);
        } catch (error) {
            message.error('Không thể tải nội dung tài liệu');
            setContent('Lỗi tải nội dung...');
        } finally {
            setContentLoading(false);
        }
    };

    const columns = [
        {
            title: 'Tên tài liệu',
            dataIndex: 'title',
            key: 'title',
            render: (text: string) => (
                <Space size="middle">
                    <FilePdfOutlined style={{ fontSize: 20, color: '#ff4d4f' }} />
                    <div style={{ maxWidth: 350 }}>
                        <Text strong ellipsis>{text}</Text>
                    </div>
                </Space>
            )
        },
        {
            title: 'Loại',
            dataIndex: 'documentType',
            key: 'documentType',
            render: (type: string) => <Tag color="blue">{type || 'Essay'}</Tag>
        },
        {
            title: 'Môn học',
            dataIndex: 'subjectName',
            key: 'subjectName',
            render: (val: string) => val || <Text type="secondary">N/A</Text>
        },
        // Only show author if not student
        ...(!isStudent ? [{
            title: 'Tác giả',
            dataIndex: 'userName',
            key: 'userName',
            render: (text: string) => <Space><UserOutlined style={{ fontSize: 12 }} />{text}</Space>
        }] : []),
        {
            title: 'Ngày tải lên',
            dataIndex: 'uploadDate',
            key: 'uploadDate',
            render: (date: string) => <Space><CalendarOutlined style={{ fontSize: 12 }} />{new Date(date).toLocaleDateString('vi-VN')}</Space>
        },
        {
            title: 'Thao tác',
            key: 'action',
            render: (_: any, record: DocumentDto) => (
                <Space size="middle">
                    <Tooltip title="Kiểm tra đạo văn">
                        <Button
                            type="text"
                            icon={<FileSearchOutlined style={{ color: '#722ed1' }} />}
                            onClick={() => navigate('/check', { state: { sourceDocId: record.id, fileName: record.title } })}
                        />
                    </Tooltip>
                    <Tooltip title="Xem nội dung">
                        <Button type="text" icon={<EyeOutlined />} onClick={() => handleViewDetail(record)} />
                    </Tooltip>
                    <Tooltip title="Tải xuống">
                        <Button type="text" icon={<DownloadOutlined />} onClick={() => handleDownload(record.id)} />
                    </Tooltip>
                    <Tooltip title="Xóa">
                        <Button type="text" danger icon={<DeleteOutlined />} onClick={() => handleDelete(record.id)} />
                    </Tooltip>
                </Space>
            ),
        },
    ];

    const filteredDocs = documents.filter(d =>
        d.title.toLowerCase().includes(searchText.toLowerCase()) ||
        d.userName.toLowerCase().includes(searchText.toLowerCase())
    );

    return (
        <div className="animate-fade-in">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
                <Title level={2} className="gradient-text">{isStudent ? 'Tài liệu của tôi' : 'Kho tài liệu lưu trữ'}</Title>
                <Space>
                    <Input
                        placeholder={isStudent ? "Tìm kiếm tài liệu..." : "Tìm kiếm tài liệu, tác giả..."}
                        prefix={<SearchOutlined />}
                        style={{ width: 300 }}
                        onChange={e => setSearchText(e.target.value)}
                    />
                    {isStudent && (
                        <Button type="primary" icon={<UploadOutlined />} onClick={() => setUploadVisible(true)}>
                            Tải lên tài liệu
                        </Button>
                    )}
                    <Button icon={<DownloadOutlined />}>Xuất danh sách</Button>
                </Space>
            </div>

            <Card className="glass-card" bordered={false}>
                <Table
                    columns={columns}
                    dataSource={filteredDocs}
                    rowKey="id"
                    loading={loading}
                    pagination={{ pageSize: 10, showSizeChanger: true }}
                />
            </Card>

            {/* Batch Upload Modal */}
            <Modal
                title="Tải lên tài liệu mới"
                open={uploadVisible}
                onCancel={() => !uploadingFiles && setUploadVisible(false)}
                footer={[
                    <Button key="cancel" onClick={() => setUploadVisible(false)} disabled={uploadingFiles}>
                        Hủy
                    </Button>,
                    <Button
                        key="upload"
                        type="primary"
                        loading={uploadingFiles}
                        onClick={handleBatchUpload}
                    >
                        Bắt đầu tải lên
                    </Button>
                ]}
            >
                <div style={{ marginBottom: 16 }}>
                    <Text type="secondary">
                        Bạn có thể tải lên một hoặc nhiều file (PDF, DOCX, TXT).
                        Hệ thống sẽ tự động lưu vào kho tài liệu của bạn.
                    </Text>
                </div>
                <Dragger
                    multiple
                    fileList={fileList}
                    onRemove={(file) => {
                        const index = fileList.indexOf(file);
                        const newFileList = fileList.slice();
                        newFileList.splice(index, 1);
                        setFileList(newFileList);
                    }}
                    beforeUpload={(file) => {
                        setFileList([...fileList, file]);
                        return false; // Prevent automatic upload
                    }}
                >
                    <p className="ant-upload-drag-icon">
                        <InboxOutlined />
                    </p>
                    <p className="ant-upload-text">Nhấp hoặc kéo thả file vào vùng này để tải lên</p>
                    <p className="ant-upload-hint">
                        Hỗ trợ tải lên hàng loạt. File của bạn sẽ được bảo mật trong kho lưu trữ cá nhân.
                    </p>
                </Dragger>
            </Modal>

            {/* Document Detail Modal */}
            <Modal
                title={<Space><FileTextOutlined /> Chi tiết tài liệu</Space>}
                open={detailVisible}
                onCancel={() => setDetailVisible(false)}
                width={1000}
                footer={[
                    <Button key="close" onClick={() => setDetailVisible(false)}>Đóng</Button>,
                    <Button
                        key="download"
                        type="primary"
                        icon={<DownloadOutlined />}
                        onClick={() => selectedDoc && handleDownload(selectedDoc.id)}
                    >
                        Tải xuống
                    </Button>,
                    <Button
                        key="print"
                        icon={<FileTextOutlined />}
                        onClick={handlePrint}
                    >
                        In báo cáo
                    </Button>

                ]}
            >
                {selectedDoc && (
                    <div style={{ minHeight: 400 }}>
                        <Title level={4}>{selectedDoc.title}</Title>
                        <Space split={<Divider type="vertical" />}>
                            <Text type="secondary"><UserOutlined /> Tác giả: {selectedDoc.userName}</Text>
                            <Tag color="blue">{selectedDoc.documentType}</Tag>
                            <Text type="secondary"><CalendarOutlined /> Ngày tải: {new Date(selectedDoc.uploadDate).toLocaleString('vi-VN')}</Text>
                        </Space>

                        <Divider orientation="left">Nội dung trích xuất</Divider>

                        <div style={{
                            maxHeight: 500,
                            overflowY: 'auto',
                            padding: 24,
                            background: '#f9f9f9',
                            borderRadius: 8,
                            border: '1px solid #eee',
                            fontFamily: 'serif',
                            fontSize: 16,
                            lineHeight: 1.8
                        }}>
                            {contentLoading ? (
                                <div style={{ textAlign: 'center', padding: 50 }}><Spin tip="Đang tải nội dung..." /></div>
                            ) : (
                                <Paragraph style={{ whiteSpace: 'pre-wrap' }}>
                                    {content}
                                </Paragraph>
                            )}
                        </div>
                    </div>
                )}
            </Modal>
        </div>
    );
};

export default DocumentListPage;
