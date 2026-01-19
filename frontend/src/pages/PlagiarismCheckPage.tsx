import React, { useState } from 'react';
import { Card, Upload, message, Button, Typography, Steps, Row, Col, Progress, List, Tag, Divider, Space, Badge, Statistic } from 'antd';
import { InboxOutlined, FileSearchOutlined, CheckCircleOutlined, InfoCircleOutlined, EyeOutlined, WarningOutlined, ArrowLeftOutlined } from '@ant-design/icons';
import { motion } from 'framer-motion';
import documentApi from '../api/documentApi';
import plagiarismApi from '../api/plagiarismApi';

const { Dragger } = Upload;
const { Title, Text, Paragraph } = Typography;

interface Match {
    id: number;
    source: string;
    similarity: number;
    text: string;
    startIndex: number;
    endIndex: number;
    severity: 'high' | 'medium' | 'low';
}

const PlagiarismCheckPage: React.FC = () => {
    const [currentStep, setCurrentStep] = useState(0);
    const [uploading, setUploading] = useState(false);
    const [result, setResult] = useState<any>(null);
    const [fullText, setFullText] = useState<string>("");
    const [activeMatchId, setActiveMatchId] = useState<number | null>(null);
    const [pendingFile, setPendingFile] = useState<File | null>(null);
    const [pendingFileName, setPendingFileName] = useState<string>("");
    const [selectedMatch, setSelectedMatch] = useState<Match | null>(null);

    const resetAnalysis = () => {
        setCurrentStep(0);
        setResult(null);
        setFullText("");
        setPendingFile(null);
        setPendingFileName("");
        setActiveMatchId(null);
        setSelectedMatch(null);
        setUploading(false);
    };

    const beforeUpload = (file: File) => {
        const isSupported = file.name.toLowerCase().endsWith('.txt') ||
            file.name.toLowerCase().endsWith('.docx') ||
            file.name.toLowerCase().endsWith('.pdf');

        if (!isSupported) {
            message.warning("Định dạng file không hỗ trợ. Vui lòng chọn .txt, .docx hoặc .pdf");
            return false;
        }

        setPendingFile(file);
        setPendingFileName(file.name);
        message.success(`Đã chọn file ${file.name} thành công!`);

        return false; // Stop Ant Design from performing a real POST request
    };

    const handlePrint = () => {
        window.print();
    };

    const startAnalysis = async () => {
        if (!pendingFile) return;

        setUploading(true);
        setCurrentStep(1);

        try {
            // 1. Upload document and extract text
            const uploadResult = await documentApi.upload({
                file: pendingFile,
                title: pendingFileName,
                documentType: 'Essay',
                isPublic: false
            });

            // Get the extracted content to show on screen
            const contentResult = await documentApi.getContent(uploadResult.id);
            setFullText(contentResult.content);

            // 2. Start plagiarism check
            const checkResult = await plagiarismApi.check({
                sourceDocumentId: uploadResult.id,
                notes: `Checked from web UI: ${pendingFileName}`
            });

            // 3. Map backend results to frontend format
            const segments = checkResult.detailedAnalysis.segments.map((seg, index) => ({
                id: index,
                text: seg.text,
                score: seg.score,
                source: seg.source,
                matchedText: seg.matchedText,
                isExcluded: seg.isExcluded,
                exclusionReason: seg.exclusionReason,
                severity: seg.score > 60 ? 'high' : (seg.score > 30 ? 'medium' : 'low')
            }));

            const matches = checkResult.matchDetails.map((m, idx) => ({
                id: idx,
                source: m.matchedDocumentTitle,
                similarity: m.similarityPercentage,
                text: m.textMatches?.[0]?.matchedText || "",
                author: m.author
            }));

            setResult({
                score: checkResult.overallSimilarityPercentage,
                matchedDocs: checkResult.totalMatchedDocuments,
                detailedAnalysis: { segments },
                matches: matches
            });

            setCurrentStep(2);
        } catch (error: any) {
            message.error(`Lỗi khi kiểm tra đạo văn: ${error}`);
            setCurrentStep(0);
        } finally {
            setUploading(false);
        }
    };

    const renderComparisonSource = () => {
        if (!selectedMatch) return null;

        return (
            <div className="comparison-source-panel animate-fade-in">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                    <Title level={5} style={{ margin: 0 }}>Nguồn đối soát trong kho</Title>
                    <Button type="link" icon={<ArrowLeftOutlined />} onClick={() => setSelectedMatch(null)}>
                        Quay lại danh sách
                    </Button>
                </div>

                <Card size="small" style={{ background: '#e6f7ff', border: '1px solid #91d5ff', marginBottom: 12 }}>
                    <Text strong>{selectedMatch.source}</Text>
                    <br />
                    <Tag color="blue" style={{ marginTop: 4 }}>Độ tương đồng: {selectedMatch.similarity}%</Tag>
                </Card>

                <Title level={5} style={{ fontSize: 13, color: '#8c8c8c' }}>VĂN BẢN TRONG KHO DỮ LIỆU:</Title>
                <div className="source-content-view">
                    <Paragraph>
                        ... [Phần nội dung trước đó của tài liệu nguồn] ...
                        <br /><br />
                        <span className="source-match-highlight">
                            {selectedMatch.text}
                        </span>
                        <br /><br />
                        ... [Phần nội dung tiếp theo của tài liệu nguồn để so sánh ngữ cảnh] ...
                    </Paragraph>
                </div>

                <div style={{ marginTop: 20 }}>
                    <Text type="secondary" italic style={{ fontSize: 12 }}>
                        <InfoCircleOutlined /> Bạn đang xem so sánh trực tiếp với tài liệu lưu trữ tại hệ thống BAU.
                    </Text>
                </div>
            </div>
        );
    };

    const renderDetailedAnalysis = () => {
        if (!result?.detailedAnalysis?.segments) return <Paragraph>{fullText}</Paragraph>;

        return result.detailedAnalysis.segments.map((seg: any, idx: number) => {
            if (seg.isExcluded) {
                return (
                    <span key={idx} style={{ color: '#bfbfbf', textDecoration: 'none' }} title={seg.exclusionReason}>
                        {seg.text}
                    </span>
                );
            }

            if (seg.score > 15) {
                const isSelected = selectedMatch && selectedMatch.source === seg.source;
                const className = `highlight-${seg.severity} ${isSelected ? 'highlight-active' : ''}`;
                return (
                    <span
                        key={idx}
                        className={className}
                        onClick={() => {
                            const match = result.matches.find((m: any) => m.source === seg.source);
                            if (match) {
                                setSelectedMatch(match);
                                setActiveMatchId(seg.id);
                            }
                        }}
                        id={`match-${seg.id}`}
                        title={`Trùng khớp ${seg.score}% từ ${seg.source}`}
                    >
                        {seg.text}
                    </span>
                );
            }

            return <span key={idx}>{seg.text}</span>;
        });
    };

    return (
        <div className="animate-fade-in">
            <Title level={2} className="gradient-text">Kiểm tra đạo văn</Title>

            <Card className="glass-card" style={{ marginBottom: 24 }}>
                <Steps
                    current={currentStep}
                    items={[
                        { title: 'Tải lên tài liệu', icon: <InboxOutlined /> },
                        { title: 'Đang phân tích', icon: <FileSearchOutlined /> },
                        { title: 'Kết quả', icon: <CheckCircleOutlined /> },
                    ]}
                    style={{ padding: '0 20px 20px' }}
                />

                {currentStep === 0 && (
                    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
                        <Dragger
                            name="file"
                            multiple={false}
                            beforeUpload={beforeUpload}
                            showUploadList={false}
                            style={{ padding: 40, background: 'rgba(24, 144, 255, 0.02)', borderRadius: 16 }}
                        >
                            <p className="ant-upload-drag-icon">
                                <Badge count={<WarningOutlined style={{ color: '#faad14' }} />}>
                                    <InboxOutlined style={{ color: '#003a8c', fontSize: 64 }} />
                                </Badge>
                            </p>
                            {pendingFileName ? (
                                <div>
                                    <Text strong style={{ fontSize: 18, color: '#1890ff' }}>
                                        <FileSearchOutlined /> {pendingFileName}
                                    </Text>
                                    <br />
                                    <Text type="secondary">File đã sẵn sàng để kiểm tra</Text>
                                </div>
                            ) : (
                                <Divider plain><Text type="secondary">Kéo thả file .docx, .pdf hoặc .txt</Text></Divider>
                            )}
                            <div style={{ display: 'flex', justifyContent: 'center', gap: 20, marginTop: 20 }}>
                                <Badge status="processing" text="Dữ liệu nội bộ BAU" />
                                <Badge status="warning" text="Cơ sở dữ liệu Internet" />
                                <Badge status="success" text="Tạp chí khoa học" />
                            </div>
                        </Dragger>

                        {pendingFile && (
                            <div style={{ textAlign: 'center', marginTop: 30 }}>
                                <Button
                                    type="primary"
                                    size="large"
                                    icon={<FileSearchOutlined />}
                                    className="gradient-btn"
                                    onClick={startAnalysis}
                                    style={{ height: 50, padding: '0 40px', fontSize: 18 }}
                                >
                                    Bắt đầu kiểm tra đạo văn
                                </Button>
                            </div>
                        )}
                    </motion.div>
                )}

                {currentStep === 1 && (
                    <div style={{ textAlign: 'center', padding: '60px 0' }}>
                        <Progress
                            type="circle"
                            percent={currentStep === 1 ? 75 : 100}
                            strokeColor={{
                                '0%': '#108ee9',
                                '100%': '#87d068',
                            }}
                            status="active"
                        />
                        <div style={{ marginTop: 24 }}>
                            <Title level={4}>Hệ thống đang đối soát dữ liệu...</Title>
                            <Text type="secondary">Chúng tôi đang quét hơn 10 triệu tài liệu trong thư viện BAU và Internet.</Text>
                        </div>
                    </div>
                )}

                {currentStep === 2 && result && (
                    <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ duration: 0.5 }}>
                        <Row gutter={[24, 24]}>
                            <Col span={24}>
                                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', background: '#f5f5f5', padding: '16px 24px', borderRadius: 8, marginBottom: 16 }}>
                                    <Space size="large">
                                        <div>
                                            <Text type="secondary">Tỷ lệ trùng khớp:</Text>
                                            <Title level={4} style={{ margin: 0, color: result.score > 20 ? '#ff4d4f' : '#52c41a' }}>{result.score}%</Title>
                                        </div>
                                        <Divider type="vertical" style={{ height: 40 }} />
                                        <div>
                                            <Text type="secondary">Nguồn trùng khớp:</Text>
                                            <Title level={4} style={{ margin: 0 }}>{result.matchedDocs} nguồn</Title>
                                        </div>
                                    </Space>
                                    <Space>
                                        <Button icon={<EyeOutlined />} onClick={handlePrint}>Xuất báo cáo (In)</Button>
                                        <Button danger type="primary" onClick={resetAnalysis}>Xoá & Kiểm tra file khác</Button>
                                    </Space>
                                </div>

                                {/* Print-only Summary Header */}
                                <div className="print-only" style={{ padding: '20px 0', borderBottom: '2px solid #003a8c', marginBottom: 30 }}>
                                    <Title level={2}>BÁO CÁO KẾT QUẢ KIỂM TRA ĐẠO VĂN</Title>
                                    <Space size="large" style={{ marginTop: 20 }}>
                                        <Statistic title="Tỷ lệ trùng khớp" value={result.score} suffix="%" valueStyle={{ color: result.score > 20 ? '#ff4d4f' : '#52c41a' }} />
                                        <Statistic title="Số nguồn trùng khớp" value={result.matchedDocs} />
                                        <Statistic title="Ngày kiểm tra" value={new Date().toLocaleDateString('vi-VN')} />
                                    </Space>
                                    <div style={{ marginTop: 20 }}>
                                        <Text strong>Tên tài liệu:</Text> <Text>{pendingFileName}</Text>
                                    </div>
                                </div>

                            </Col>

                            {/* Main Analysis Side-by-Side Area */}
                            <Col span={12}>
                                <Card title="Văn bản của bạn (Đã tải lên)" size="small" className="glass-card">
                                    <div className="plagiarism-text-container">
                                        {renderDetailedAnalysis()}
                                    </div>
                                </Card>
                            </Col>

                            <Col span={12}>
                                {selectedMatch ? (
                                    renderComparisonSource()
                                ) : (
                                    <>
                                        <Card title="Danh sách các nguồn trùng khớp" size="small" className="glass-card">
                                            <List
                                                dataSource={result.matches}
                                                renderItem={(item: Match) => (
                                                    <List.Item
                                                        style={{
                                                            cursor: 'pointer',
                                                            padding: '12px',
                                                            borderRadius: 8,
                                                            marginBottom: 8,
                                                            border: activeMatchId === item.id ? '1px solid #1890ff' : '1px solid transparent',
                                                            background: activeMatchId === item.id ? '#e6f7ff' : 'transparent'
                                                        }}
                                                        onClick={() => {
                                                            setSelectedMatch(item);
                                                            // For highlighting on the left
                                                            const matchIndex = result.detailedAnalysis.segments.findIndex((s: any) => s.source === item.source);
                                                            if (matchIndex !== -1) {
                                                                setActiveMatchId(matchIndex);
                                                                const element = document.getElementById(`match-${matchIndex}`);
                                                                element?.scrollIntoView({ behavior: 'smooth', block: 'center' });
                                                            }
                                                        }}
                                                    >
                                                        <List.Item.Meta
                                                            title={
                                                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                                                    <Text strong style={{ color: '#1890ff' }}>{item.source}</Text>
                                                                    <Tag color={item.similarity > 50 ? 'red' : item.similarity > 20 ? 'orange' : 'green'}>
                                                                        {item.similarity}%
                                                                    </Tag>
                                                                </div>
                                                            }
                                                            description={<Paragraph type="secondary" ellipsis={{ rows: 2 }}>{item.text}</Paragraph>}
                                                        />
                                                    </List.Item>
                                                )}
                                            />
                                        </Card>

                                        <Card style={{ marginTop: 24, background: '#fffbe6', border: '1px solid #ffe58f' }}>
                                            <Space direction="vertical">
                                                <Text strong><InfoCircleOutlined color="#faad14" /> Chú thích mức độ:</Text>
                                                <Space>
                                                    <Tag color="red">Nghiêm trọng (&gt;50%)</Tag>
                                                    <Tag color="orange">Trung bình (20-50%)</Tag>
                                                    <Tag color="green">Thấp (&lt;20%)</Tag>
                                                </Space>
                                            </Space>
                                        </Card>
                                    </>
                                )}
                            </Col>
                        </Row>
                    </motion.div>
                )}
            </Card>

            <Card className="glass-card" title={<Space><InfoCircleOutlined style={{ color: '#faad14' }} /> <Text>Quy định về đạo văn tại BAU</Text></Space>}>
                <Paragraph>
                    Theo quy định của Học viện Ngân hàng, các sản phẩm học thuật có tỷ lệ trùng khớp <strong>trên 20%</strong> sẽ bị đánh giá là không đạt.
                    Tính năng <strong>So sánh trực tiếp</strong> giúp giảng viên và sinh viên đối chiếu chính xác đoạn văn bị trùng với tài liệu gốc trong kho lưu trữ.
                </Paragraph>
            </Card>
        </div>
    );
};

export default PlagiarismCheckPage;
