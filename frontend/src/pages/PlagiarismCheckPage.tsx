import React, { useState, useEffect } from 'react';
import { useLocation, useParams } from 'react-router-dom';
import { Card, Upload, message, Button, Typography, Steps, Row, Col, Progress, List, Tag, Divider, Space, Badge, Statistic } from 'antd';
import { InboxOutlined, FileSearchOutlined, CheckCircleOutlined, InfoCircleOutlined, EyeOutlined, WarningOutlined, ArrowLeftOutlined, DownloadOutlined, FileTextOutlined, HistoryOutlined, ClockCircleOutlined } from '@ant-design/icons';
import { motion } from 'framer-motion';
import documentApi from '../api/documentApi';
import plagiarismApi from '../api/plagiarismApi';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '../store';
import { updateCredits, logout } from '../store/slices/authSlice';

const { Dragger } = Upload;
const { Title, Text, Paragraph } = Typography;

interface Match {
    id: number;
    source: string;
    similarity: number;
    text: string;
    startIndex?: number;
    endIndex?: number;
    severity?: 'high' | 'medium' | 'low';
    author?: string;
}

const PlagiarismCheckPage: React.FC = () => {
    const { user } = useSelector((state: RootState) => state.auth);
    const dispatch = useDispatch();
    const [currentStep, setCurrentStep] = useState(0);
    const [uploading, setUploading] = useState(false);
    const [result, setResult] = useState<any>(null);
    const [sourceDocId, setSourceDocId] = useState<number | null>(null);
    const [fullText, setFullText] = useState<string>("");
    const [activeMatchId, setActiveMatchId] = useState<number | null>(null);
    const [pendingFile, setPendingFile] = useState<File | null>(null);
    const [pendingFileName, setPendingFileName] = useState<string>("");
    const [selectedMatch, setSelectedMatch] = useState<Match | null>(null);
    const [history, setHistory] = useState<any[]>([]);
    const [historyLoading, setHistoryLoading] = useState(false);
    const [progress, setProgress] = useState(0);
    const [loadingStatus, setLoadingStatus] = useState("Đang khởi tạo...");
    const [checkInfo, setCheckInfo] = useState<any>(null);
    const location = useLocation();
    const { id } = useParams<{ id: string }>();

    useEffect(() => {
        fetchHistory();
        if (id) {
            viewDetailFromHistory(parseInt(id));
        }
    }, [id]);

    const fetchHistory = async () => {
        setHistoryLoading(true);
        try {
            const data = await plagiarismApi.getHistory({ limit: 5 });
            setHistory(data);
        } catch (error) {
            console.error('Error fetching history:', error);
        } finally {
            setHistoryLoading(false);
        }
    };

    const viewDetailFromHistory = async (checkId: number) => {
        setUploading(true);
        setCurrentStep(1);
        setProgress(30);
        setLoadingStatus("Đang tải kết quả từ lịch sử...");

        try {
            const detail = await plagiarismApi.getDetail(checkId);
            setProgress(60);

            // Get original document text
            const contentResult = await documentApi.getContent(detail.sourceDocumentId);
            setFullText(contentResult.content);
            setPendingFileName(detail.sourceDocumentTitle);
            setSourceDocId(detail.sourceDocumentId);

            // Map detail results to frontend format
            const segments = (detail.detailedAnalysis?.segments || []).map((seg: any, index: number) => ({
                id: index,
                text: seg.text,
                score: seg.score,
                source: seg.source,
                matchedText: seg.matchedText,
                isExcluded: seg.isExcluded,
                exclusionReason: seg.exclusionReason,
                severity: seg.score > 60 ? 'high' : (seg.score > 30 ? 'medium' : 'low')
            }));

            const matches = (detail.matches || []).map((m: any, idx: number) => ({
                id: idx,
                source: m.matchedDocumentTitle,
                similarity: m.similarityScore,
                text: m.matchedText,
                author: "",
                severity: m.similarityScore > 50 ? 'high' : (m.similarityScore > 20 ? 'medium' : 'low')
            }));

            setResult({
                score: detail.overallSimilarityPercentage,
                matchedDocs: detail.totalMatchedDocuments,
                detailedAnalysis: { segments },
                matches: matches
            });

            setCheckInfo({
                userName: detail.userName,
                checkDate: detail.checkDate,
                fileName: detail.sourceDocumentTitle
            });

            setProgress(100);
            setLoadingStatus("Hoàn tất!");

            setTimeout(() => {
                setCurrentStep(2);
            }, 500);

        } catch (error) {
            console.error('Error loading detail:', error);
            message.error('Không thể tải chi tiết kết quả');
            setCurrentStep(0);
        } finally {
            setUploading(false);
        }
    };

    useEffect(() => {
        const state = location.state as { sourceDocId?: number, fileName?: string };
        if (state?.sourceDocId) {
            setSourceDocId(state.sourceDocId);
            setPendingFileName(state.fileName || "Tài liệu hệ thống");
            startAnalysisFromId(state.sourceDocId, state.fileName || "Tài liệu hệ thống");
        }
    }, [location.state]);

    const startAnalysisFromId = async (id: number, fileName: string) => {
        setUploading(true);
        setCurrentStep(1);
        setProgress(10);
        setLoadingStatus("Đang truy xuất tài liệu từ hệ thống...");

        const interval = setInterval(() => {
            setProgress(prev => {
                if (prev < 40) return prev + 3;
                if (prev < 70) return prev + 2;
                if (prev < 95) return prev + 1;
                return prev;
            });
        }, 600);

        try {
            // 2. Perform Plagiarism Analysis directly
            setLoadingStatus("Đang so khớp với 10 triệu+ tài liệu trong thư viện BAU và Internet...");
            const checkResult = await plagiarismApi.check({
                sourceDocumentId: id,
                notes: `Checked from web UI (Library): ${fileName}`
            });

            // 3. Get document text content
            setLoadingStatus("Đang tải dữ liệu văn bản...");
            const contentResult = await documentApi.getContent(id);
            setFullText(contentResult.content);
            setPendingFileName(fileName);

            // 4. Map backend results to frontend format
            setLoadingStatus("Đang phân tích cấu trúc trùng khớp...");
            setProgress(90);
            const segments = (checkResult.detailedAnalysis?.segments || []).map((seg: any, index: number) => ({
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

            setCheckInfo({
                userName: user?.fullName,
                checkDate: new Date().toISOString(),
                fileName: pendingFileName
            });

            clearInterval(interval);
            setProgress(100);
            setLoadingStatus("Hoàn tất!");

            setTimeout(() => {
                setCurrentStep(2);
                fetchHistory(); // Refresh history
                // Update global credits state
                dispatch(updateCredits({
                    remainingChecksToday: checkResult.remainingChecksToday,
                    dailyCheckLimit: checkResult.dailyCheckLimit
                }));
            }, 500);
        } catch (error: any) {
            clearInterval(interval);
            console.error('Analysis error:', error);
            message.error(error.message || 'Lỗi phân tích tài liệu');
            setCurrentStep(0);
        } finally {
            setUploading(false);
        }
    };

    const resetAnalysis = () => {
        setCurrentStep(0);
        setResult(null);
        setFullText("");
        setPendingFile(null);
        setPendingFileName("");
        setActiveMatchId(null);
        setSelectedMatch(null);
        setUploading(false);
        setSourceDocId(null);
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

    const downloadTextReport = () => {
        if (!result) return;

        let reportText = `BÁO CÁO KẾT QUẢ KIỂM TRA ĐẠO VĂN\n`;
        reportText += `--------------------------------\n`;
        reportText += `Tên tài liệu: ${pendingFileName}\n`;
        reportText += `Ngày kiểm tra: ${new Date().toLocaleString('vi-VN')}\n`;
        reportText += `Tỷ lệ trùng khớp: ${result.score}%\n`;
        reportText += `Số nguồn trùng khớp: ${result.matchedDocs}\n\n`;
        reportText += `DANH SÁCH CÁC NGUỒN TRÙNG KHỚP:\n`;

        result.matches.forEach((m: any, idx: number) => {
            reportText += `${idx + 1}. [${m.similarity}%] ${m.source}\n`;
        });

        const blob = new Blob([reportText], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Bao_Cao_Dao_Van_${pendingFileName.replace(/\.[^/.]+$/, "")}.txt`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    };

    const startAnalysis = async () => {
        if (!pendingFile) return;

        setUploading(true);
        setCurrentStep(1);
        setProgress(5);
        setLoadingStatus("Đang chuẩn bị tệp tin...");

        const interval = setInterval(() => {
            setProgress(prev => {
                if (prev < 30) return prev + 5;
                if (prev < 60) return prev + 2;
                if (prev < 92) return prev + 1;
                return prev;
            });
        }, 500);

        try {
            // 1. Upload document (as inactive so it doesn't show in library)
            setLoadingStatus("Đang tải tài liệu lên máy chủ BAU...");
            const uploadResult = await documentApi.upload({
                file: pendingFile,
                title: pendingFileName,
                documentType: 'Essay',
                isPublic: false,
                isActive: false // Don't show in repository
            });
            setSourceDocId(uploadResult.id);
            setProgress(35);

            // Get the extracted content to show on screen
            setLoadingStatus("Đang trích xuất nội dung văn bản...");
            const contentResult = await documentApi.getContent(uploadResult.id);
            setFullText(contentResult.content);
            setProgress(50);

            // 2. Start plagiarism check
            setLoadingStatus("Đang đối soát dữ liệu với các nguồn học thuật...");
            const checkResult = await plagiarismApi.check({
                sourceDocumentId: uploadResult.id,
                notes: `Checked from web UI: ${pendingFileName}`
            });

            // 3. Map backend results to frontend format
            setLoadingStatus("Đang xử lý kết quả phân tích...");
            setProgress(90);
            const segments = (checkResult.detailedAnalysis?.segments || []).map((seg: any, index: number) => ({
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

            setCheckInfo({
                userName: user?.fullName,
                checkDate: new Date().toISOString(),
                fileName: pendingFileName
            });

            clearInterval(interval);
            setProgress(100);
            setLoadingStatus("Sẵn sàng hiển thị kết quả!");

            setTimeout(() => {
                setCurrentStep(2);
                fetchHistory(); // Refresh history
                // Update global credits state
                dispatch(updateCredits({
                    remainingChecksToday: checkResult.remainingChecksToday,
                    dailyCheckLimit: checkResult.dailyCheckLimit
                }));
            }, 600);
        } catch (error: any) {
            clearInterval(interval);
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
                            const match = (result?.matches || []).find((m: any) => m.source === seg.source);
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

                        {user?.role === 'Student' && (
                            <div style={{ marginTop: 24, textAlign: 'center' }}>
                                <Card size="small" style={{ display: 'inline-block', background: (user.remainingChecksToday || 0) > 0 ? '#f6ffed' : '#fff2e8', border: 'none' }}>
                                    <Space>
                                        <ClockCircleOutlined />
                                        <Text>Lượt kiểm tra hôm nay: </Text>
                                        <Text strong style={{ color: (user.remainingChecksToday || 0) > 0 ? '#52c41a' : '#ff4d4f' }}>
                                            {user.remainingChecksToday ?? 0}/{user.dailyCheckLimit ?? 5}
                                        </Text>
                                    </Space>
                                </Card>
                            </div>
                        )}

                        {pendingFile && (
                            <div style={{ textAlign: 'center', marginTop: 30 }}>
                                <Button
                                    type="primary"
                                    size="large"
                                    icon={<FileSearchOutlined />}
                                    className="gradient-btn"
                                    onClick={startAnalysis}
                                    style={{ height: 50, padding: '0 40px', fontSize: 18 }}
                                    disabled={user?.role === 'Student' && (user.remainingChecksToday === 0)}
                                >
                                    {user?.role === 'Student' && user.remainingChecksToday === 0 ? "Đã hết lượt kiểm tra hôm nay" : "Bắt đầu kiểm tra đạo văn"}
                                </Button>
                            </div>
                        )}
                        {/* Recent History Section */}
                        {history.length > 0 && (
                            <div style={{ marginTop: 40, textAlign: 'left' }}>
                                <Divider orientation="left">
                                    <Space><HistoryOutlined /> Lịch sử kiểm tra gần đây</Space>
                                </Divider>
                                <List
                                    loading={historyLoading}
                                    dataSource={history}
                                    renderItem={(item) => (
                                        <List.Item
                                            className="glass-card"
                                            style={{ marginBottom: 12, padding: 15, border: '1px solid #f0f0f0', borderRadius: 8, background: '#fff' }}
                                            actions={[
                                                <Button
                                                    type="primary"
                                                    ghost
                                                    icon={<EyeOutlined />}
                                                    onClick={() => viewDetailFromHistory(item.id)}
                                                >
                                                    Xem lại
                                                </Button>
                                            ]}
                                        >
                                            <List.Item.Meta
                                                avatar={<ClockCircleOutlined style={{ color: '#8c8c8c', marginTop: 4 }} />}
                                                title={<Text strong>{item.sourceDocumentTitle}</Text>}
                                                description={
                                                    <Space split={<Divider type="vertical" />}>
                                                        <Text type="secondary" style={{ fontSize: 12 }}>
                                                            {new Date(item.checkDate).toLocaleString('vi-VN')}
                                                        </Text>
                                                        <Tag color={item.overallSimilarityPercentage > 20 ? 'volcano' : 'green'}>
                                                            {item.overallSimilarityPercentage.toFixed(1)}% Trùng khớp
                                                        </Tag>
                                                    </Space>
                                                }
                                            />
                                        </List.Item>
                                    )}
                                />
                            </div>
                        )}
                    </motion.div>
                )}

                {currentStep === 1 && (
                    <div style={{ textAlign: 'center', padding: '60px 0' }}>
                        <Progress
                            type="circle"
                            percent={progress}
                            strokeColor={{
                                '0%': '#108ee9',
                                '100%': '#87d068',
                            }}
                            status="active"
                        />
                        <div style={{ marginTop: 24 }}>
                            <Title level={4}>{loadingStatus}</Title>
                            <Text type="secondary">
                                {progress < 40 ? "Đang chuẩn bị dữ liệu..." :
                                    progress < 80 ? "Chúng tôi đang so khớp với hàng triệu tài liệu..." :
                                        "Sắp hoàn tất, đang tổng hợp báo cáo chi tiết..."}
                            </Text>
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
                                        {checkInfo && (
                                            <>
                                                <Divider type="vertical" style={{ height: 40 }} />
                                                <div>
                                                    <Text type="secondary">Người nộp:</Text>
                                                    <div style={{ fontWeight: 'bold' }}>{checkInfo.userName}</div>
                                                </div>
                                                <Divider type="vertical" style={{ height: 40 }} />
                                                <div>
                                                    <Text type="secondary">Ngày nộp:</Text>
                                                    <div style={{ fontSize: 13 }}>{new Date(checkInfo.checkDate).toLocaleString('vi-VN')}</div>
                                                </div>
                                            </>
                                        )}
                                    </Space>
                                    <Space>
                                        <Button icon={<DownloadOutlined />} onClick={() => sourceDocId && window.open(documentApi.getDownloadUrl(sourceDocId), '_blank')}>Tải xuống file gốc</Button>
                                        <Button icon={<FileTextOutlined />} onClick={downloadTextReport}>Tải báo cáo chi tiết</Button>
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
                                                            const matchIndex = (result?.detailedAnalysis?.segments || []).findIndex((s: any) => s.source === item.source);
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
