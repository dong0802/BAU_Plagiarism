import React, { useState, useEffect } from 'react';
import { useLocation, useParams } from 'react-router-dom';
import { Card, Upload, message, Button, Typography, Steps, Row, Col, Progress, List, Tag, Divider, Space, Badge, Statistic, Modal, Input, Radio, Form } from 'antd';
import { InboxOutlined, FileSearchOutlined, CheckCircleOutlined, InfoCircleOutlined, EyeOutlined, WarningOutlined, ArrowLeftOutlined, ArrowRightOutlined, DownloadOutlined, FileTextOutlined, HistoryOutlined, ClockCircleOutlined, UserOutlined, FileSearchOutlined as FileIcon } from '@ant-design/icons';
import { motion } from 'framer-motion';
import documentApi from '../api/documentApi';
import plagiarismApi from '../api/plagiarismApi';
import qualityApi, { DocumentQualityAnalysis } from '../api/qualityApi';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '../store';
import { updateCredits, logout } from '../store/slices/authSlice';
import QualityAnalysisModal from '../components/QualityAnalysisModal';

const { Dragger } = Upload;
const { Title, Text, Paragraph } = Typography;

interface IPlagiarismMatch {
    id: number;
    source: string;
    similarity: number;
    text: string;
    allSnippets?: string[]; // All matched segments from this source
    fullContent?: string;
    matchedDocumentId?: number; // New field for robust fetching
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
    const [selectedMatch, setSelectedMatch] = useState<IPlagiarismMatch | null>(null);
    const [history, setHistory] = useState<any[]>([]);
    const [historyLoading, setHistoryLoading] = useState(false);
    const [progress, setProgress] = useState(0);
    const [loadingStatus, setLoadingStatus] = useState("Đang khởi tạo...");
    const [checkInfo, setCheckInfo] = useState<any>(null);
    const [isAiModalVisible, setIsAiModalVisible] = useState(false);
    const [inputType, setInputType] = useState<'file' | 'text'>('file');
    const [pastedText, setPastedText] = useState("");
    const [qualityAnalysis, setQualityAnalysis] = useState<DocumentQualityAnalysis | null>(null);
    const [isQualityModalVisible, setIsQualityModalVisible] = useState(false);
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
        setLoadingStatus("Đang tải kết quả...");

        try {
            let detail = await plagiarismApi.getDetail(checkId);

            // Polling if still processing
            if (detail.status === "Processing") {
                detail = await pollForResult(checkId);
            }

            if (detail.status === "Failed") {
                throw new Error(detail.notes || "Phân tích thất bại");
            }

            setProgress(80);
            setLoadingStatus("Đang tải dữ liệu văn bản...");

            // Get original document text
            const contentResult = await documentApi.getContent(detail.sourceDocumentId);
            setFullText(contentResult.content);
            setPendingFileName(detail.sourceDocumentTitle);
            setSourceDocId(detail.sourceDocumentId);

            mapBackendResult(detail);

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

        } catch (error: any) {
            console.error('Error loading detail:', error);
            message.error(error.message || 'Không thể tải chi tiết kết quả');
            setCurrentStep(0);
        } finally {
            setUploading(false);
        }
    };

    const pollForResult = async (checkId: number): Promise<any> => {
        setLoadingStatus("Hệ thống đang phân tích sâu (AI & Đạo văn)...");
        let attempts = 0;
        const maxAttempts = 60; // 90 seconds (60 * 1.5s)

        while (attempts < maxAttempts) {
            const detail = await plagiarismApi.getDetail(checkId);
            if (detail.status !== "Processing") return detail;

            attempts++;

            // Better progress feedback
            if (attempts < 20) {
                setLoadingStatus("Đang quét kho dữ liệu nội bộ...");
                setProgress(prev => Math.min(93, prev + 0.5));
            } else if (attempts < 40) {
                setLoadingStatus("Đang phân tích AI và ngữ nghĩa...");
                setProgress(prev => Math.min(96, prev + 0.2));
            } else {
                setLoadingStatus("Đang hoàn thiện kết quả phân tích...");
                setProgress(prev => Math.min(98, prev + 0.1));
            }

            await new Promise(resolve => setTimeout(resolve, 1500));
        }

        throw new Error("Quá thời gian xử lý. Vui lòng kiểm tra lại trong Lịch sử sau vài phút.");
    };

    const mapBackendResult = (detail: any) => {
        // Map detail results to frontend format
        let totalWordsCount = 0;
        const sourceWordsMatched: { [key: string]: number } = {};

        const segments = (detail.detailedAnalysis?.segments || []).map((seg: any, index: number) => {
            const wordCount = seg.text ? seg.text.trim().split(/\s+/).filter((w: string) => w.length > 0).length : 0;
            totalWordsCount += wordCount;

            // If this segment is part of a match (> 40%), add its word count to that source
            if (seg.source && seg.score > 40) {
                sourceWordsMatched[seg.source] = (sourceWordsMatched[seg.source] || 0) + wordCount;
            }

            return {
                id: index,
                text: seg.text,
                score: seg.score,
                source: seg.source,
                matchedText: seg.matchedText,
                isExcluded: seg.isExcluded,
                exclusionReason: seg.exclusionReason,
                severity: seg.score > 60 ? 'high' : (seg.score > 30 ? 'medium' : 'low') as 'high' | 'medium' | 'low'
            };
        });

        // Consolidation: Calculate document contribution % (matches professional logic)
        let consolidatedMatches: IPlagiarismMatch[] = [];
        const seenDocs = new Set<string>();
        const docSnippets: { [key: string]: string[] } = {}; // To store all matched snippets for global highlighting

        // Pre-parse all matches to group snippets by local document title
        (detail.matches || []).forEach((m: any) => {
            const title = m.matchedDocumentTitle || m.MatchedDocumentTitle || "N/A";
            const txt = m.matchedText || m.MatchedText || "";
            if (txt && txt.trim().length > 5) {
                if (!docSnippets[title]) docSnippets[title] = [];
                if (!docSnippets[title].includes(txt)) docSnippets[title].push(txt);
            }
        });

        (detail.matches || []).forEach((m: any, idx: number) => {
            // Handle potential casing variations from backend
            const docTitle = m.matchedDocumentTitle || m.MatchedDocumentTitle || "N/A";
            const docFullContent = m.fullContent || m.FullContent || "";
            const docAuthorName = m.authorName || m.AuthorName || m.author || m.Author || "N/A";
            const docMatchedText = m.matchedText || m.MatchedText || "";

            if (!seenDocs.has(docTitle)) {
                seenDocs.add(docTitle);

                // Calculate Doc-level % contribution
                const matchedWords = sourceWordsMatched[docTitle] || 0;
                const docContributionPercent = totalWordsCount > 0
                    ? parseFloat(((matchedWords / totalWordsCount) * 100).toFixed(1))
                    : 0;

                const docId = m.matchedDocumentId || m.MatchedDocumentId || 0;

                consolidatedMatches.push({
                    id: idx,
                    source: docTitle,
                    similarity: docContributionPercent,
                    text: docMatchedText,
                    allSnippets: docSnippets[docTitle] || [], // Pass all snippets for this doc
                    fullContent: docFullContent,
                    matchedDocumentId: docId, // Map the ID for fallback loading
                    author: docAuthorName,
                    severity: docContributionPercent > 20 ? 'high' : (docContributionPercent > 5 ? 'medium' : 'low') as any
                });
            }
        });

        // Lọc bỏ những nguồn có tỷ lệ trùng khớp quá nhỏ (< 1%) để danh sách gọn gàng
        consolidatedMatches = consolidatedMatches.filter(m => m.similarity >= 1.0);

        // Sort by contribution %
        consolidatedMatches.sort((a, b) => b.similarity - a.similarity);

        setResult({
            score: detail.overallSimilarityPercentage,
            matchedDocs: consolidatedMatches.length,
            detailedAnalysis: { segments },
            matches: consolidatedMatches,
            aiProbability: detail.aiProbability,
            aiDetectionLevel: detail.aiDetectionLevel,
            aiAnalysis: detail.aiAnalysis
        });
    };

    const renderAiDetailsModal = () => {
        if (!result?.aiAnalysis) return null;

        return (
            <Modal
                title={
                    <Space>
                        <WarningOutlined style={{ color: '#faad14' }} />
                        <span>Chi tiết phân tích trí tuệ nhân tạo (AI)</span>
                    </Space>
                }
                open={isAiModalVisible}
                onCancel={() => setIsAiModalVisible(false)}
                footer={[
                    <Button key="close" onClick={() => setIsAiModalVisible(false)}>Đóng</Button>
                ]}
                width={window.innerWidth < 768 ? '95%' : 850}
                className="ai-details-modal"
                style={{ top: 20 }}
            >
                <div style={{ marginBottom: 25 }}>
                    <Row gutter={20} style={{ marginBottom: 20 }}>
                        <Col span={8}>
                            <Card size="small" style={{ background: '#f0f5ff', border: '1px solid #adc6ff' }}>
                                <Statistic
                                    title="Xác suất AI"
                                    value={result.aiProbability}
                                    suffix="%"
                                    valueStyle={{ color: result.aiProbability > 70 ? '#cf1322' : '#389e0d', fontWeight: 'bold' }}
                                />
                                <Progress
                                    percent={result.aiProbability}
                                    size="small"
                                    status={result.aiProbability > 70 ? "exception" : "active"}
                                    showInfo={false}
                                />
                            </Card>
                        </Col>
                        <Col span={8}>
                            <Card size="small" style={{ background: '#f9f0ff', border: '1px solid #d3adf7' }}>
                                <Statistic
                                    title="Perplexity (Độ đo dễ đoán)"
                                    value={result.aiAnalysis.perplexity}
                                    suffix="/100"
                                    precision={1}
                                    valueStyle={{ color: result.aiAnalysis.perplexity < 40 ? '#cf1322' : '#389e0d' }}
                                />
                                <Text type="secondary" style={{ fontSize: 11 }}>
                                    {result.aiAnalysis.perplexity < 40 ? "⚠️ Rất dễ đoán (Dấu hiệu AI)" : "✅ Từ vựng phong phú"}
                                </Text>
                            </Card>
                        </Col>
                        <Col span={8}>
                            <Card size="small" style={{ background: '#fff7e6', border: '1px solid #ffd591' }}>
                                <Statistic
                                    title="Burstiness (Độ biến thiên)"
                                    value={result.aiAnalysis.burstiness}
                                    suffix="/100"
                                    precision={1}
                                    valueStyle={{ color: result.aiAnalysis.burstiness < 30 ? '#cf1322' : '#389e0d' }}
                                />
                                <Text type="secondary" style={{ fontSize: 11 }}>
                                    {result.aiAnalysis.burstiness < 30 ? "⚠️ Cấu trúc đều (Dấu hiệu AI)" : "✅ Cấu trúc linh hoạt"}
                                </Text>
                            </Card>
                        </Col>
                    </Row>

                    <div style={{ background: '#f5f5f5', padding: '15px', borderRadius: '8px', borderLeft: '4px solid #1890ff' }}>
                        <Text strong style={{ display: 'block', marginBottom: 5 }}>Tổng quan đánh giá:</Text>
                        <Paragraph style={{ margin: 0, fontSize: 15 }}>
                            {result.aiAnalysis.summary}
                        </Paragraph>
                    </div>
                </div>

                <Divider orientation="left" style={{ fontSize: 14, fontWeight: 600 }}>PHÂN TÍCH TỪNG ĐOẠN VĂN</Divider>

                <div style={{ maxHeight: '400px', overflowY: 'auto', padding: '10px', background: '#fafafa', borderRadius: '8px', border: '1px solid #f0f0f0' }}>
                    {result.aiAnalysis.sentences?.map((item: any, idx: number) => (
                        <div
                            key={idx}
                            style={{
                                padding: '12px',
                                marginBottom: '10px',
                                borderRadius: '8px',
                                background: item.aiProbability > 70 ? '#fff1f0' : (item.aiProbability > 40 ? '#fffbe6' : '#ffffff'),
                                border: `1px solid ${item.aiProbability > 70 ? '#ffa39e' : (item.aiProbability > 40 ? '#ffe58f' : '#f0f0f0')}`,
                                boxShadow: '0 2px 4px rgba(0,0,0,0.02)'
                            }}
                        >
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '6px' }}>
                                <Space>
                                    <Badge count={idx + 1} style={{ backgroundColor: '#8c8c8c' }} />
                                    <Text strong style={{ fontSize: 13 }}>Đoạn văn {idx + 1}</Text>
                                </Space>
                                <Tag color={item.aiProbability > 70 ? 'red' : (item.aiProbability > 40 ? 'gold' : 'green')}>
                                    Xác suất AI: {item.aiProbability}%
                                </Tag>
                            </div>
                            <Text style={{ fontSize: 14, lineHeight: '1.6' }}>{item.text}</Text>
                        </div>
                    ))}
                </div>

                <div style={{ marginTop: 20, textAlign: 'center' }}>
                    <Text type="secondary" style={{ fontSize: '12px' }}>
                        <InfoCircleOutlined /> Công cụ sử dụng phương pháp phân tích Perplexity và Burstiness để nhận diện văn bản máy tính tạo ra.
                    </Text>
                </div>
            </Modal>
        );
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
        setLoadingStatus("Đang tạo yêu cầu phân tích...");

        try {
            const checkRequest = await plagiarismApi.check({
                sourceDocumentId: id,
                notes: `Checked from web UI (Library): ${fileName}`
            });

            const checkResult = await pollForResult(checkRequest.checkId);

            // 3. Get document text content
            setLoadingStatus("Đang tải dữ liệu văn bản...");
            const contentResult = await documentApi.getContent(id);
            setFullText(contentResult.content);
            setPendingFileName(fileName);

            // 4. Map backend results to frontend format
            setLoadingStatus("Đang xử lý kết quả...");
            mapBackendResult(checkResult);

            setCheckInfo({
                userName: user?.fullName,
                checkDate: new Date().toISOString(),
                fileName: pendingFileName
            });

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
        setPastedText("");
        setInputType('file');
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
        if (inputType === 'file' && !pendingFile) return;
        if (inputType === 'text' && !pastedText.trim()) {
            message.warning("Vui lòng dán nội dung văn bản cần kiểm tra");
            return;
        }

        setUploading(true);
        setCurrentStep(1);
        setProgress(5);
        setLoadingStatus("Đang khởi tạo phiên làm việc...");

        const interval = setInterval(() => {
            setProgress(prev => {
                if (prev < 30) return prev + 5;
                if (prev < 60) return prev + 2;
                if (prev < 92) return prev + 1;
                return prev;
            });
        }, 500);

        try {
            let docId: number;
            let displayTitle: string = pendingFileName;

            if (inputType === 'file' && pendingFile) {
                // 1. Tải tài liệu lên
                setLoadingStatus("Đang tải tài liệu lên máy chủ BAU...");
                const uploadResult = await documentApi.upload({
                    file: pendingFile,
                    title: pendingFileName,
                    documentType: 'Essay',
                    isPublic: false,
                    isActive: false
                });
                docId = uploadResult.id;
                displayTitle = uploadResult.title;
            } else {
                // 1. Tạo từ văn bản dán
                setLoadingStatus("Đang xử lý văn bản nội dung...");
                const titleFromText = pastedText.substring(0, 50).trim() + (pastedText.length > 50 ? "..." : "");
                const textResult = await documentApi.createFromText({
                    content: pastedText,
                    title: `Văn bản dán_${new Date().getTime()}`,
                    documentType: 'Essay',
                    isPublic: false,
                    isActive: false
                });
                docId = textResult.id;
                displayTitle = titleFromText;
            }

            setSourceDocId(docId);
            setProgress(35);

            // Lấy nội dung đã trích xuất để hiển thị trên màn hình
            setLoadingStatus("Đang chuẩn bị dữ liệu hiển thị...");
            const contentResult = await documentApi.getContent(docId);
            setFullText(contentResult.content);
            setProgress(50);

            // 2. Bắt đầu kiểm tra đạo văn
            setLoadingStatus("Đang đối soát dữ liệu (Đạo văn & AI)...");
            const checkRequest = await plagiarismApi.check({
                sourceDocumentId: docId,
                notes: `Kiểm tra từ giao diện Web (${inputType}): ${displayTitle}`
            });

            // Xóa bộ đếm tiến trình "giả" và để pollForResult đảm nhận tiến trình thực từ máy chủ
            clearInterval(interval);
            const checkResult = await pollForResult(checkRequest.checkId);

            // 3. Chuyển đổi kết quả backend sang định dạng frontend
            setLoadingStatus("Đang xử lý kết quả phân tích...");
            mapBackendResult(checkResult);

            // 4. Thực hiện việc phân tích chất lượng
            setLoadingStatus("Đang phân tích chất lượng văn bản...");
            try {
                const qualityResult = await qualityApi.analyzeDocument(docId);
                setQualityAnalysis(qualityResult);
            } catch (error) {
                console.error('Lỗi phân tích chất lượng:', error);
                // Tiếp tục ngay cả khi phân tích chất lượng thất bại
            }

            setCheckInfo({
                userName: user?.fullName,
                checkDate: new Date().toISOString(),
                fileName: displayTitle
            });

            clearInterval(interval);
            setProgress(100);
            setLoadingStatus("Sẵn sàng hiển thị kết quả!");

            setTimeout(() => {
                setCurrentStep(2);
                fetchHistory(); // Làm mới lịch sử
                // Cập nhật trạng thái lượt kiểm tra toàn cục
                dispatch(updateCredits({
                    remainingChecksToday: checkResult.remainingChecksToday,
                    dailyCheckLimit: checkResult.dailyCheckLimit
                }));
            }, 600);
        } catch (error: any) {
            clearInterval(interval);
            console.error('Plagiarism check error:', error);
            message.error(error.message || `Lỗi khi kiểm tra đạo văn: ${error}`);
            setCurrentStep(0);
        } finally {
            setUploading(false);
        }
    };

    const SourceComparisonBox: React.FC<{ match: IPlagiarismMatch, onClose: () => void }> = ({ match, onClose }) => {
        const [sourceContent, setSourceContent] = useState<string | null>(match.fullContent || null);
        const [loading, setLoading] = useState(!match.fullContent && !!match.matchedDocumentId);

        useEffect(() => {
            if (!match.fullContent && match.matchedDocumentId) {
                setLoading(true);
                documentApi.getContent(match.matchedDocumentId)
                    .then(res => {
                        setSourceContent(res.content);
                    })
                    .catch(e => {
                        console.error("Failed to fetch source content:", e);
                    })
                    .finally(() => setLoading(false));
            } else {
                setSourceContent(match.fullContent || null);
                setLoading(false);
            }
        }, [match.matchedDocumentId, match.fullContent]);

        // Multi-segment highlighter logic
        const renderHighlightedSource = (text: string) => {
            if (!text) return null;

            const activeSnippet = match.text;
            const otherSnippets = (match.allSnippets || []).filter(s => s !== activeSnippet);

            return text.split('\n').map((line, lineIdx) => {
                if (!line.trim()) return <br key={lineIdx} />;

                let fragments: { text: string, type: 'active' | 'other' | 'none' }[] = [{ text: line, type: 'none' }];

                // 1. Mark active snippet
                if (activeSnippet && line.includes(activeSnippet)) {
                    let newFrags: typeof fragments = [];
                    fragments.forEach(f => {
                        if (f.type !== 'none') { newFrags.push(f); return; }
                        const parts = f.text.split(activeSnippet);
                        parts.forEach((p, pIdx) => {
                            if (p) newFrags.push({ text: p, type: 'none' });
                            if (pIdx < parts.length - 1) newFrags.push({ text: activeSnippet, type: 'active' });
                        });
                    });
                    fragments = newFrags;
                }

                // 2. Mark other snippets from same source
                otherSnippets.forEach(snippet => {
                    if (!line.includes(snippet)) return;
                    let newFrags: typeof fragments = [];
                    fragments.forEach(f => {
                        if (f.type !== 'none') { newFrags.push(f); return; }
                        const parts = f.text.split(snippet);
                        parts.forEach((p, pIdx) => {
                            if (p) newFrags.push({ text: p, type: 'none' });
                            if (pIdx < parts.length - 1) newFrags.push({ text: snippet, type: 'other' });
                        });
                    });
                    fragments = newFrags;
                });

                return (
                    <React.Fragment key={lineIdx}>
                        {fragments.map((frag, fragIdx) => (
                            <span
                                key={fragIdx}
                                className={frag.type === 'active' ? 'highlight-active' : (frag.type === 'other' ? 'highlight-secondary' : '')}
                                title={frag.type === 'active' ? 'Đoạn văn Đang chọn đối chiếu' : (frag.type === 'other' ? 'Đoạn văn trùng khớp từ nguồn này' : undefined)}
                            >
                                {frag.text}
                            </span>
                        ))}
                        <br />
                    </React.Fragment>
                );
            });
        };

        return (
            <div className="comparison-source-panel animate-fade-in" style={{ position: 'sticky', top: 20 }}>
                <div style={{ padding: '0 0 12px 0', borderBottom: '3px solid #1890ff', marginBottom: 20, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                        <div style={{ width: 10, height: 10, borderRadius: '50%', background: '#1890ff' }}></div>
                        <Text strong style={{ color: '#1890ff', fontSize: 13, textTransform: 'uppercase', letterSpacing: '1px' }}>
                            TÀI LIỆU ĐỐI SOÁT CHI TIẾT
                        </Text>
                    </div>
                    <Button type="primary" danger ghost size="small" icon={<ArrowRightOutlined />} onClick={onClose}>
                        Đóng xem nguồn
                    </Button>
                </div>

                <Card
                    className="glass-card"
                    bodyStyle={{ padding: 16 }}
                    style={{ marginBottom: 20, border: '1px solid #bae7ff' }}
                >
                    <div style={{ display: 'flex', justifyContent: 'space-between', gap: 15 }}>
                        <div style={{ flex: 1 }}>
                            <Title level={5} style={{ color: '#003a8c', marginBottom: 4 }}>{match.source}</Title>
                            <Space split={<Divider type="vertical" />}>
                                {match.author && <Text type="secondary" style={{ fontSize: 12 }}><UserOutlined /> {match.author}</Text>}
                                <Tag color="blue" icon={<FileSearchOutlined />}>Đang so khớp</Tag>
                            </Space>
                        </div>
                        <div style={{ textAlign: 'right' }}>
                            <Statistic
                                title="Góp mặt hệ thống"
                                value={match.similarity}
                                suffix="%"
                                valueStyle={{ color: '#1890ff', fontSize: 22, fontWeight: 'bold' }}
                            />
                        </div>
                    </div>
                </Card>

                <div className="plagiarism-word-container custom-scrollbar" style={{ height: 'calc(100vh - 420px)', minHeight: 450, padding: '20px', background: '#f8fafc' }}>
                    <div className="word-page" style={{
                        padding: '60px 80px',
                        minHeight: '100%',
                        fontSize: '15.5px',
                        border: '1px solid #e2e8f0',
                        boxShadow: '0 10px 25px rgba(0,0,0,0.05)'
                    }}>
                        <div style={{ color: '#94a3b8', marginBottom: 40, fontSize: '12px', textAlign: 'center', borderBottom: '1px solid #f1f5f9', paddingBottom: '15px', fontWeight: 600 }}>
                            {match.source.toUpperCase()} - TOÀN VĂN
                        </div>

                        {loading ? (
                            <div style={{ textAlign: 'center', padding: '100px 0' }}>
                                <Badge status="processing" text="Đang đồng bộ dữ liệu toàn phần..." />
                            </div>
                        ) : sourceContent ? (
                            <Paragraph style={{
                                whiteSpace: 'pre-wrap',
                                lineHeight: '1.9',
                                textAlign: 'justify',
                                color: '#2c3e50',
                                fontFamily: '"Times New Roman", Times, serif'
                            }}>
                                {renderHighlightedSource(sourceContent)}
                            </Paragraph>
                        ) : (
                            <div style={{ textAlign: 'center', padding: '80px 40px', background: '#fff', borderRadius: 12 }}>
                                <InboxOutlined style={{ fontSize: 48, color: '#d9d9d9', marginBottom: 20 }} />
                                <Paragraph style={{ fontSize: 16, color: '#595959' }}>
                                    {match.text}
                                </Paragraph>
                                <Divider />
                                <Text type="secondary" italic>
                                    Đang tải nội dung gốc...
                                </Text>
                            </div>
                        )}
                        <div style={{ color: '#94a3b8', marginTop: 50, fontSize: '11px', textAlign: 'center', borderTop: '1px solid #f1f5f9', paddingTop: '20px', fontStyle: 'italic' }}>
                            --- Kết thúc văn bản đối soát ---
                        </div>
                    </div>
                </div>

                <div style={{ marginTop: 24, display: 'flex', gap: 10 }}>
                    <div style={{ flex: 1, padding: '10px 15px', background: '#e6f7ff', borderRadius: 8, border: '1px solid #91d5ff', display: 'flex', alignItems: 'center', gap: 8 }}>
                        <div style={{ width: 12, height: 12, background: '#3b82f6', borderRadius: 2 }}></div>
                        <Text style={{ fontSize: 11, color: '#0050b3' }}>Đang chọn</Text>
                    </div>
                    <div style={{ flex: 1, padding: '10px 15px', background: '#fffbeb', borderRadius: 8, border: '1px solid #fde68a', display: 'flex', alignItems: 'center', gap: 8 }}>
                        <div style={{ width: 12, height: 12, background: '#fbbf24', borderRadius: 2 }}></div>
                        <Text style={{ fontSize: 11, color: '#92400e' }}>Trùng lặp khác</Text>
                    </div>
                </div>
            </div>
        );
    };

    const renderComparisonSource = () => {
        if (!selectedMatch) return null;
        return <SourceComparisonBox match={selectedMatch} onClose={() => setSelectedMatch(null)} />;
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
        <>
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
                            <div style={{ textAlign: 'center', marginBottom: 30 }}>
                                <Radio.Group
                                    value={inputType}
                                    onChange={(e) => setInputType(e.target.value)}
                                    buttonStyle="solid"
                                    size="large"
                                >
                                    <Radio.Button value="file"><InboxOutlined /> Tải lên File</Radio.Button>
                                    <Radio.Button value="text"><FileTextOutlined /> Dán văn bản</Radio.Button>
                                </Radio.Group>
                            </div>

                            {inputType === 'file' ? (
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
                            ) : (
                                <div style={{ padding: '0 20px' }}>
                                    <Input.TextArea
                                        placeholder="Dán nội dung văn bản bạn muốn kiểm tra vào đây (Hỗ trợ lên đến 1000 từ)..."
                                        value={pastedText}
                                        onChange={(e) => setPastedText(e.target.value)}
                                        rows={15}
                                        className="custom-scrollbar"
                                        style={{ borderRadius: 12, padding: 16, fontSize: 15, border: '2px solid #e6f7ff' }}
                                    />
                                    <div style={{ marginTop: 10, display: 'flex', justifyContent: 'space-between' }}>
                                        <Text type="secondary" italic>Mẹo: Bạn có thể dán toàn bộ bài luận hoặc báo cáo dài vào đây.</Text>
                                        <Space>
                                            <Text type={pastedText.trim().split(/\s+/).filter(w => w.length > 0).length > 1000 ? 'danger' : 'secondary'}>
                                                Số từ: {pastedText.trim().split(/\s+/).filter(w => w.length > 0).length} / 1000
                                            </Text>
                                            <Divider type="vertical" />
                                            <Text type="secondary">Ký tự: {pastedText.length}</Text>
                                        </Space>
                                    </div>
                                </div>
                            )}

                            {/* Đã bỏ giới hạn lượt kiểm tra cho sinh viên */}

                            {((inputType === 'file' && pendingFile) || (inputType === 'text' && pastedText.trim().length > 100)) && (
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
                                            "Sắp hoàn tất, đang tổng hợp kết quả phân tích..."}
                                </Text>
                            </div>
                        </div>
                    )}

                    {currentStep === 2 && result && (
                        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ duration: 0.5 }}>
                            <Row gutter={[24, 24]}>
                                <Col span={24}>
                                    {/* Modern Result Header */}
                                    <Card
                                        className="glass-card"
                                        style={{
                                            marginBottom: 24,
                                            background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                                            border: 'none',
                                            overflow: 'hidden'
                                        }}
                                        bodyStyle={{ padding: 0 }}
                                    >
                                        <Row>
                                            {/* Left side - Statistics */}
                                            <Col xs={24} lg={14} style={{ padding: '32px', borderRight: '1px solid rgba(255,255,255,0.1)' }}>
                                                <Row gutter={[24, 24]}>
                                                    <Col xs={24} sm={12}>
                                                        <div style={{ textAlign: 'center' }}>
                                                            <div style={{
                                                                fontSize: 48,
                                                                fontWeight: 'bold',
                                                                color: '#fff',
                                                                textShadow: '0 2px 4px rgba(0,0,0,0.2)'
                                                            }}>
                                                                {result.score}%
                                                            </div>
                                                            <div style={{ color: 'rgba(255,255,255,0.9)', fontSize: 16, marginTop: 8 }}>
                                                                Tỷ lệ trùng khớp
                                                            </div>
                                                            <Tag
                                                                color={result.score > 20 ? 'error' : 'success'}
                                                                style={{ marginTop: 12, fontSize: 13, padding: '4px 12px' }}
                                                            >
                                                                {result.score > 20 ? '⚠️ Nguy cơ cao' : '✅ An toàn'}
                                                            </Tag>
                                                        </div>
                                                    </Col>
                                                    <Col xs={24} sm={12}>
                                                        <div style={{ textAlign: 'center' }}>
                                                            <div style={{
                                                                fontSize: 48,
                                                                fontWeight: 'bold',
                                                                color: '#fff',
                                                                textShadow: '0 2px 4px rgba(0,0,0,0.2)'
                                                            }}>
                                                                {result.matchedDocs}
                                                            </div>
                                                            <div style={{ color: 'rgba(255,255,255,0.9)', fontSize: 16, marginTop: 8 }}>
                                                                Nguồn trùng khớp
                                                            </div>
                                                            <Tag
                                                                color="processing"
                                                                style={{ marginTop: 12, fontSize: 13, padding: '4px 12px' }}
                                                            >
                                                                📚 {result.matchedDocs} tài liệu
                                                            </Tag>
                                                        </div>
                                                    </Col>
                                                </Row>

                                                {checkInfo && (
                                                    <Row gutter={[16, 16]} style={{ marginTop: 24, paddingTop: 24, borderTop: '1px solid rgba(255,255,255,0.1)' }}>
                                                        <Col xs={24} sm={12}>
                                                            <Space direction="vertical" size={4}>
                                                                <Text style={{ color: 'rgba(255,255,255,0.7)', fontSize: 12 }}>👤 Người nộp</Text>
                                                                <Text strong style={{ color: '#fff', fontSize: 15 }}>{checkInfo.userName}</Text>
                                                            </Space>
                                                        </Col>
                                                        <Col xs={24} sm={12}>
                                                            <Space direction="vertical" size={4}>
                                                                <Text style={{ color: 'rgba(255,255,255,0.7)', fontSize: 12 }}>📅 Ngày nộp</Text>
                                                                <Text strong style={{ color: '#fff', fontSize: 15 }}>
                                                                    {new Date(checkInfo.checkDate).toLocaleDateString('vi-VN')}
                                                                </Text>
                                                            </Space>
                                                        </Col>
                                                    </Row>
                                                )}
                                            </Col>

                                            {/* Right side - Actions */}
                                            <Col xs={24} lg={10} style={{ padding: '32px', background: 'rgba(255,255,255,0.05)' }}>
                                                <div style={{ marginBottom: 16 }}>
                                                    <Text strong style={{ color: '#fff', fontSize: 16, display: 'block', marginBottom: 16 }}>
                                                        ⚡ Thao tác nhanh
                                                    </Text>
                                                </div>
                                                <Space direction="vertical" size={12} style={{ width: '100%' }}>
                                                    <Button
                                                        block
                                                        size="large"
                                                        icon={<DownloadOutlined />}
                                                        onClick={() => sourceDocId && window.open(documentApi.getDownloadUrl(sourceDocId), '_blank')}
                                                        style={{
                                                            background: 'rgba(255,255,255,0.15)',
                                                            border: '1px solid rgba(255,255,255,0.3)',
                                                            color: '#fff',
                                                            fontWeight: 500
                                                        }}
                                                    >
                                                        Tải xuống file gốc
                                                    </Button>
                                                    <Button
                                                        block
                                                        size="large"
                                                        icon={<EyeOutlined />}
                                                        onClick={handlePrint}
                                                        style={{
                                                            background: 'rgba(255,255,255,0.15)',
                                                            border: '1px solid rgba(255,255,255,0.3)',
                                                            color: '#fff',
                                                            fontWeight: 500
                                                        }}
                                                    >
                                                        Xuất báo cáo (In)
                                                    </Button>
                                                    {qualityAnalysis && (
                                                        <Button
                                                            block
                                                            size="large"
                                                            type="primary"
                                                            icon={<CheckCircleOutlined />}
                                                            onClick={() => setIsQualityModalVisible(true)}
                                                            style={{
                                                                background: '#52c41a',
                                                                borderColor: '#52c41a',
                                                                fontWeight: 500
                                                            }}
                                                        >
                                                            Xem phân tích chất lượng
                                                        </Button>
                                                    )}
                                                    <Divider style={{ borderColor: 'rgba(255,255,255,0.2)', margin: '8px 0' }} />
                                                    <Button
                                                        block
                                                        size="large"
                                                        danger
                                                        type="primary"
                                                        onClick={resetAnalysis}
                                                        style={{ fontWeight: 500 }}
                                                    >
                                                        🔄 Kiểm tra file khác
                                                    </Button>
                                                </Space>
                                            </Col>
                                        </Row>
                                    </Card>

                                    {/* AI Detection Result Card */}
                                    {result.aiProbability !== undefined && (
                                        <Card
                                            size="small"
                                            style={{
                                                marginBottom: 24,
                                                background: result.aiProbability > 70 ? '#fff1f0' : (result.aiProbability > 40 ? '#fffbe6' : '#f6ffed'),
                                                border: `1px solid ${result.aiProbability > 70 ? '#ffa39e' : (result.aiProbability > 40 ? '#ffe58f' : '#b7eb8f')}`
                                            }}
                                        >
                                            <Row align="middle" gutter={[16, 16]}>
                                                <Col xs={24} sm={6}>
                                                    <Statistic
                                                        title={<Space><WarningOutlined /> Xác suất AI</Space>}
                                                        value={result.aiProbability}
                                                        suffix="%"
                                                        valueStyle={{
                                                            color: result.aiProbability > 70 ? '#cf1322' : (result.aiProbability > 40 ? '#d48806' : '#389e0d'),
                                                            fontWeight: 'bold',
                                                            fontSize: window.innerWidth < 768 ? 20 : 24
                                                        }}
                                                    />
                                                </Col>
                                                <Col xs={0} sm={1}>
                                                    <Divider type="vertical" style={{ height: 40 }} />
                                                </Col>
                                                <Col xs={24} sm={12}>
                                                    <div style={{ display: 'flex', flexDirection: 'column' }}>
                                                        <Text strong style={{ fontSize: 16 }}>
                                                            Mức độ nghi ngờ: <Tag color={result.aiProbability > 70 ? 'red' : (result.aiProbability > 40 ? 'gold' : 'green')}>{result.aiDetectionLevel}</Tag>
                                                        </Text>
                                                        <Text type="secondary" style={{ fontSize: 13 }}>{result.aiAnalysis?.summary || "Đang phân tích chi tiết..."}</Text>
                                                    </div>
                                                </Col>
                                                <Col xs={24} sm={5}>
                                                    <Button type="primary" ghost block onClick={() => setIsAiModalVisible(true)}>Chi tiết AI</Button>
                                                </Col>
                                            </Row>
                                        </Card>
                                    )}

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
                                <Col xs={24} lg={selectedMatch ? 12 : 15}>
                                    <Card
                                        title={<Title level={5} style={{ margin: 0, color: '#003a8c' }}><FileTextOutlined /> {selectedMatch ? "BẢN GỐC (BÀI NỘP CỦA BẠN)" : "Nội dung bài nộp (Định dạng văn bản)"}</Title>}
                                        size="small"
                                        className="glass-card"
                                        headStyle={{ background: '#f8fafc', borderBottom: '1px solid #e2e8f0', padding: '12px 20px' }}
                                        bodyStyle={{ padding: 0 }}
                                    >
                                        <div className="plagiarism-word-container custom-scrollbar">
                                            <div className="word-page">
                                                {renderDetailedAnalysis()}
                                            </div>
                                        </div>
                                    </Card>
                                </Col>

                                <Col xs={24} lg={selectedMatch ? 12 : 9}>
                                    {selectedMatch ? (
                                        renderComparisonSource()
                                    ) : (
                                        <div className="matches-list-container">
                                            <Card
                                                title={
                                                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
                                                        <Space><HistoryOutlined /> Nguồn trùng khớp</Space>
                                                        <Tag color="processing">{result.matches.length} nguồn</Tag>
                                                    </div>
                                                }
                                                size="small"
                                                className="glass-card"
                                                bodyStyle={{ padding: '8px' }}
                                            >
                                                <div className="custom-scrollbar" style={{ maxHeight: 'calc(100vh - 450px)', overflowY: 'auto' }}>
                                                    <List<any>
                                                        dataSource={result.matches || []}
                                                        renderItem={(item: any, index: number) => {
                                                            const isSelected = !!selectedMatch && (selectedMatch as any).source === item.source;
                                                            return (
                                                                <div
                                                                    className={`match-item-premium ${isSelected ? 'active' : ''}`}
                                                                    style={{
                                                                        padding: '12px 16px',
                                                                        borderRadius: '12px',
                                                                        marginBottom: '10px',
                                                                        cursor: 'pointer',
                                                                        transition: 'all 0.3s ease',
                                                                        border: isSelected ? '1px solid #1890ff' : '1px solid #f0f0f0',
                                                                        background: isSelected ? '#f0f7ff' : '#fff',
                                                                        position: 'relative',
                                                                        overflow: 'hidden'
                                                                    }}
                                                                    onClick={() => {
                                                                        setSelectedMatch(item);
                                                                        // Highlight segments belonging to this source
                                                                        const segments = result?.detailedAnalysis?.segments || [];
                                                                        const matchIndex = segments.findIndex((s: any) => s.source === item.source);
                                                                        if (matchIndex !== -1) {
                                                                            setActiveMatchId(matchIndex);
                                                                            const element = document.getElementById(`match-${matchIndex}`);
                                                                            element?.scrollIntoView({ behavior: 'smooth', block: 'center' });
                                                                        }
                                                                    }}
                                                                >
                                                                    <div style={{ display: 'flex', gap: '12px', alignItems: 'flex-start' }}>
                                                                        <div style={{
                                                                            width: '32px',
                                                                            height: '32px',
                                                                            borderRadius: '8px',
                                                                            background: item.similarity > 50 ? '#fff1f0' : (item.similarity > 20 ? '#fffbe6' : '#f6ffed'),
                                                                            display: 'flex',
                                                                            alignItems: 'center',
                                                                            justifyContent: 'center',
                                                                            border: `1px solid ${item.similarity > 50 ? '#ffa39e' : (item.similarity > 20 ? '#ffe58f' : '#b7eb8f')}`,
                                                                            flexShrink: 0
                                                                        }}>
                                                                            <Text strong style={{ color: item.similarity > 50 ? '#cf1322' : (item.similarity > 20 ? '#d48806' : '#389e0d') }}>
                                                                                {index + 1}
                                                                            </Text>
                                                                        </div>

                                                                        <div style={{ flex: 1, minWidth: 0 }}>
                                                                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '4px' }}>
                                                                                <Text strong style={{ fontSize: '14px', color: isSelected ? '#1890ff' : '#262626' }} ellipsis title={item.source}>
                                                                                    {item.source}
                                                                                </Text>
                                                                                <Text strong style={{ color: item.similarity > 50 ? '#cf1322' : (item.similarity > 20 ? '#d48806' : '#389e0d'), marginLeft: '8px' }}>
                                                                                    {item.similarity}%
                                                                                </Text>
                                                                            </div>

                                                                            <div style={{ marginBottom: '8px' }}>
                                                                                <Progress
                                                                                    percent={item.similarity}
                                                                                    showInfo={false}
                                                                                    size="small"
                                                                                    strokeColor={item.similarity > 50 ? '#ff4d4f' : (item.similarity > 20 ? '#faad14' : '#52c41a')}
                                                                                    trailColor="#f0f0f0"
                                                                                />
                                                                            </div>

                                                                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                                                                <Space size={4}>
                                                                                    <Tag color="blue" style={{ fontSize: '10px', margin: 0, padding: '0 4px', lineHeight: '16px' }}>Hệ thống BAU</Tag>
                                                                                    {item.author && <Text type="secondary" style={{ fontSize: '11px' }}>• {item.author}</Text>}
                                                                                </Space>
                                                                                {isSelected && <Text style={{ fontSize: '11px', fontWeight: 600, color: '#1890ff' }}>Đang xem <ArrowRightOutlined style={{ fontSize: 10 }} /></Text>}
                                                                            </div>
                                                                        </div>
                                                                    </div>
                                                                </div>
                                                            );
                                                        }}
                                                    />
                                                </div>
                                            </Card>

                                            <Card size="small" style={{ marginTop: 16, borderRadius: '12px', background: '#fafafa', border: '1px solid #f0f0f0' }}>
                                                <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap', justifyContent: 'center' }}>
                                                    <Space size={4}><div style={{ width: 8, height: 8, borderRadius: '50%', background: '#ff4d4f' }}></div> <Text type="secondary" style={{ fontSize: 12 }}>Rất cao (&gt;50%)</Text></Space>
                                                    <Space size={4}><div style={{ width: 8, height: 8, borderRadius: '50%', background: '#faad14' }}></div> <Text type="secondary" style={{ fontSize: 12 }}>Trung bình (20-50%)</Text></Space>
                                                    <Space size={4}><div style={{ width: 8, height: 8, borderRadius: '50%', background: '#52c41a' }}></div> <Text type="secondary" style={{ fontSize: 12 }}>Thấp (&lt;20%)</Text></Space>
                                                </div>
                                            </Card>
                                        </div>
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
                {renderAiDetailsModal()}
                <QualityAnalysisModal
                    visible={isQualityModalVisible}
                    onClose={() => setIsQualityModalVisible(false)}
                    analysis={qualityAnalysis}
                />
            </div>
        </>
    );
};

export default PlagiarismCheckPage;
