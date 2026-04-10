import React, { useState, useEffect, useRef } from 'react';
import { useLocation, useParams } from 'react-router-dom';
import { Card, Upload, message, Button, Typography, Steps, Row, Col, Progress, List, Tag, Divider, Space, Badge, Statistic, Modal, Input, Radio, Form, Checkbox, InputNumber, Spin } from 'antd';
import { InboxOutlined, FileSearchOutlined, CheckCircleOutlined, InfoCircleOutlined, EyeOutlined, WarningOutlined, ArrowLeftOutlined, ArrowRightOutlined, DownloadOutlined, FileTextOutlined, HistoryOutlined, ClockCircleOutlined, UserOutlined, CloseOutlined, FilterOutlined, AppstoreOutlined as LayersOutlined } from '@ant-design/icons';
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
    
    // Turnitin-style Sidebar & Filtering states
    const [sidePanelVisible, setSidePanelVisible] = useState(false);
    const [sidePanelType, setSidePanelType] = useState<'matches' | 'filters' | 'info' | 'none'>('matches');
    const [excludeQuotes, setExcludeQuotes] = useState(false);
    const [excludeBibliography, setExcludeBibliography] = useState(false);
    const [excludeType, setExcludeType] = useState<'none' | 'words' | 'percent'>('none');
    const [excludeMinWords, setExcludeMinWords] = useState(1);
    const [excludeMinPercent, setExcludeMinPercent] = useState(1);
    const [filteredResult, setFilteredResult] = useState<any>(null);

    const location = useLocation();
    const { id } = useParams<{ id: string }>();

    // Apply filters logic
    // Apply filters logic
    useEffect(() => {
        if (!result) {
            setFilteredResult(null);
            return;
        }

        console.log("[SIDEBAR] Applying filters and recalculating score...");

        // Start with a clone of segments
        let currentSegments = [...result.detailedAnalysis.segments];
        
        // 1. Mark segments for exclusion based on UI toggles (Quotes, Bibliography)
        currentSegments = currentSegments.map(seg => {
            let isExcluded = false;
            let reason = '';

            // Handle Bibliography
            const isBibBackend = seg.exclusionReason === 'Loại trừ Mục lục Tham khảo' || seg.isBibliography;
            if (excludeBibliography && isBibBackend) {
                isExcluded = true;
                reason = 'Loại trừ Mục lục Tham khảo';
            }

            // Handle Quotes
            if (excludeQuotes) {
                const trimmed = (seg.text || "").trim();
                const isQuoteFrontend = (trimmed.startsWith('"') && trimmed.endsWith('"')) || 
                               (trimmed.startsWith('“') && trimmed.endsWith('”')) ||
                               (trimmed.startsWith('«') && trimmed.endsWith('»')) ||
                               (trimmed.includes('"') && trimmed.length > 20 && (trimmed.match(/"/g) || []).length >= 2);
                
                if (seg.isQuote || isQuoteFrontend) {
                    isExcluded = true;
                    reason = 'Loại trừ trích dẫn';
                }
            }

            // If it was already marked as excluded by backend (other than bib)
            if (seg.isExcluded && !isBibBackend && !isExcluded) {
                isExcluded = true;
                reason = seg.exclusionReason || 'Đã loại trừ';
            }

            return { ...seg, isExcluded, exclusionReason: reason };
        });

        // 2. Aggregate matched words BY SOURCE to apply source-level filters
        const sourceWordCounts: { [key: string]: number } = {};
        let totalCountableDocWords = 0;

        currentSegments.forEach(seg => {
            const wordCount = seg.text ? seg.text.trim().split(/\s+/).filter((w: string) => w.length > 0).length : 0;
            
            // Standard Turnitin logic: 
            // - Bibliography is excluded from the total word count of the document IF the toggle is on.
            // - Quotes are included in the total word count but not the similarity sum if toggle is on.
            const shouldExcludeFromTotalCount = excludeBibliography && (seg.exclusionReason === 'Loại trừ Mục lục Tham khảo' || seg.isBibliography);
            
            if (!shouldExcludeFromTotalCount) {
                totalCountableDocWords += wordCount;
            }

            // Only count as a "match" for a source if not excluded AND has actual matching
            // Turnitin-style: score > 0 means exact word sequences were found
            if (!seg.isExcluded && seg.source && seg.score > 0) { 
                sourceWordCounts[seg.source] = (sourceWordCounts[seg.source] || 0) + wordCount;
            }
        });

        // Utility to check if segment should be ignored for similarity index
        function isExcludedSegment(seg: any) {
            return seg.isExcluded;
        }

        // 3. Filter currentMatches based on active source-level filters
        let currentMatches = result.matches.map((m: any) => {
            const matchedWords = sourceWordCounts[m.source] || 0;
            const similarity = totalCountableDocWords > 0 ? (matchedWords / totalCountableDocWords * 100) : 0;
            
            return {
                ...m,
                matchWordCount: matchedWords,
                similarity: parseFloat(similarity.toFixed(1))
            };
        });

        // Apply Source-level removal filters (Exclude small sources)
        if (excludeType === 'words') {
            currentMatches = currentMatches.filter((m: any) => m.matchWordCount >= (excludeMinWords || 1));
        } else if (excludeType === 'percent') {
            currentMatches = currentMatches.filter((m: any) => m.similarity >= (excludeMinPercent || 1));
        } else {
            // Remove matches that contribute 0% due to local segment exclusions
            currentMatches = currentMatches.filter((m: any) => m.similarity > 0);
        }

        // 4. Final Re-calculation of Overall Similarity Index
        const activeSourceTitles = new Set(currentMatches.map((m: any) => m.source));
        let matchedTotalWords = 0;

        currentSegments.forEach(seg => {
            // A word is counted in the total sum if:
            // - It matches an active source (one that wasn't filtered out by size)
            // - It is not part of an excluded segment (quote, bib)
            // - It meets the Turnitin-style threshold (score > 0 means exact match found)
            if (!isExcludedSegment(seg) && seg.source && activeSourceTitles.has(seg.source) && seg.score > 0) {
                const wordCount = seg.text ? seg.text.trim().split(/\s+/).filter((w: string) => w.length > 0).length : 0;
                matchedTotalWords += wordCount;
            }
        });

        // Tổng % đạo văn = tổng % đóng góp của các nguồn >= 1.0% (giống backend CalculateOverallScore)
        const finalScore = parseFloat(
            currentMatches
                .filter((m: any) => m.similarity >= 1.0)
                .reduce((sum: number, m: any) => sum + m.similarity, 0)
                .toFixed(1)
        );

        // 5. Update UI state with recalculated data
        setFilteredResult({
            ...result,
            score: finalScore,
            matchedDocs: currentMatches.length,
            matches: currentMatches,
            detailedAnalysis: { segments: currentSegments }
        });

    }, [result, excludeQuotes, excludeBibliography, excludeType, excludeMinWords, excludeMinPercent]);

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
        const maxAttempts = 120; // 3 phút (120 * 1.5s) để xử lý tài liệu lớn

        while (attempts < maxAttempts) {
            const detail = await plagiarismApi.getDetail(checkId);
            if (detail.status !== "Processing") return detail;

            attempts++;


            if (attempts < 20) {
                setLoadingStatus("Đang quét kho dữ liệu nội bộ...");
                setProgress(prev => Math.min(93, Math.round((prev + 0.5) * 10) / 10));
            } else if (attempts < 40) {
                setLoadingStatus("Đang phân tích AI và ngữ nghĩa...");
                setProgress(prev => Math.min(96, Math.round((prev + 0.2) * 10) / 10));
            } else if (attempts < 80) {
                setLoadingStatus("Đang hoàn thiện kết quả phân tích...");
                setProgress(prev => Math.min(98, Math.round((prev + 0.1) * 10) / 10));
            } else {
                setLoadingStatus("Tài liệu lớn, đang xử lý sâu...");
                setProgress(prev => Math.min(99, Math.round((prev + 0.05) * 100) / 100));
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

            if (!seg.isExcluded) {
                totalWordsCount += wordCount;

                // Turnitin-style: score > 0 means exact word sequences found
                if (seg.source && seg.score > 0) {
                    sourceWordsMatched[seg.source] = (sourceWordsMatched[seg.source] || 0) + wordCount;
                }
            }

            return {
                id: index,
                text: seg.text,
                score: seg.score,
                source: seg.source,
                matchedText: seg.matchedText,
                isExcluded: seg.isExcluded,
                exclusionReason: seg.exclusionReason,
                isBibliography: seg.isBibliography,
                isQuote: seg.isQuote,
                severity: seg.score > 80 ? 'high' : (seg.score > 50 ? 'medium' : 'low') as 'high' | 'medium' | 'low'
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
                    allSnippets: docSnippets[docTitle] || [],
                    fullContent: docFullContent,
                    matchedDocumentId: docId, // Map the ID for fallback loading
                    author: docAuthorName,
                    severity: docContributionPercent > 20 ? 'high' : (docContributionPercent > 5 ? 'medium' : 'low') as any
                });
            }
        });

        // Lọc bỏ những nguồn có tỷ lệ 0.0% (được làm tròn quá nhỏ) để không bị hiển thị nguồn 0% gây lỗi hiển thị
        consolidatedMatches = consolidatedMatches.filter(m => m.similarity > 0);
        // Sort by contribution %
        consolidatedMatches.sort((a, b) => b.similarity - a.similarity);

        // Tổng % đạo văn = tổng % đóng góp của các nguồn >= 1.0% (khớp với backend CalculateOverallScore)
        let exactTotalSum = 0;
        consolidatedMatches.forEach(m => {
            if (m.similarity >= 1.0) {
                exactTotalSum += m.similarity;
            }
        });
        exactTotalSum = parseFloat(exactTotalSum.toFixed(1));

        setResult({
            score: exactTotalSum,
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
                setLoadingStatus("Đang tải tài liệu lên máy chủ BAV...");
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
                fetchHistory();
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
        const contentRef = useRef<HTMLDivElement>(null);

        useEffect(() => {
            if (!match.fullContent && match.matchedDocumentId) {
                setLoading(true);
                documentApi.getContent(match.matchedDocumentId)
                    .then(res => setSourceContent(res.content))
                    .catch(e => console.error("Failed to fetch source content:", e))
                    .finally(() => setLoading(false));
            } else {
                setSourceContent(match.fullContent || null);
                setLoading(false);
            }
        }, [match.matchedDocumentId, match.fullContent]);

        useEffect(() => {
            if (!loading && sourceContent) {
                const timer = setTimeout(() => {
                    const activeElem = contentRef.current?.querySelector('.highlight-active');
                    if (activeElem) {
                        activeElem.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
                }, 300);
                return () => clearTimeout(timer);
            }
        }, [loading, sourceContent, match.text]);

        const renderHighlightedSource = (text: string) => {
            if (!text) return null;
            const activeSnippet = (match.text || "").trim();
            const otherSnippets = (match.allSnippets || []).map(s => s.trim()).filter(s => s && s !== activeSnippet && s.length > 8);
            // 1-to-1 character folding to preserve indices while allowing accent-insensitive search
            const foldChar = (c: string) => {
                const f = c.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd');
                return f.length > 0 ? f[0] : c;
            };
            
            const foldedText = text.split('').map(foldChar).join('').toLowerCase();
            const markers = new Uint8Array(text.length);

            const markSnippet = (snippet: string, markerType: number) => {
                if (!snippet || snippet.length < 4) return;
                
                // Fold the snippet as well for comparison
                const normSnippet = snippet.split('').map(foldChar).join('').toLowerCase().trim();
                
                try {
                    // Create pattern: escaping special chars and allowing any non-alphanumeric characters (like punctuation, spaces, newlines) between words.
                    // This is crucial because backend's exact-match text strips punctuation, but the rendering text still has them!
                    const pattern = normSnippet
                        .replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
                        .replace(/\s+/g, '[^a-z0-9A-Z_]+');
                    
                    const regex = new RegExp(pattern, 'gi');
                    let matchResult;
                    
                    // Search in the FOLDED text which has identical length to original
                    while ((matchResult = regex.exec(foldedText)) !== null) {
                        const start = matchResult.index;
                        const end = start + matchResult[0].length;
                        for (let i = start; i < end; i++) {
                            // Active (1) overrules Secondary (2)
                            if (markers[i] === 0 || (markerType === 1)) markers[i] = markerType;
                        }
                        if (regex.lastIndex === start) regex.lastIndex++;
                    }
                } catch (e) {
                    console.error("Marker Regex error:", e);
                }
            };
            otherSnippets.forEach(s => markSnippet(s, 2));
            if (activeSnippet) markSnippet(activeSnippet, 1);

            const elements: React.ReactNode[] = [];
            let i = 0, key = 0;
            while (i < text.length) {
                const currentType = markers[i];
                let j = i + 1;
                while (j < text.length && markers[j] === currentType) j++;
                const chunk = text.substring(i, j);
                if (currentType === 1) elements.push(<mark key={key++} className="highlight-active" style={{ background: '#bae7ff', color: '#003a8c', borderRadius: 3, padding: '1px 0', fontWeight: 600, boxShadow: '0 0 0 1px #69c0ff' }}>{chunk}</mark>);
                else if (currentType === 2) elements.push(<mark key={key++} className="highlight-secondary" style={{ background: '#fff1b8', color: '#613400', borderRadius: 3, padding: '1px 0', boxShadow: '0 0 0 1px #ffd666' }}>{chunk}</mark>);
                else {
                    elements.push(<span key={key++}>{chunk}</span>);
                }
                i = j;
            }
            return elements;
        };

        return (
            <Card
                className="comparison-source-panel animate-fade-in glass-card"
                style={{ position: 'sticky', top: 20, border: '1px solid #bae7ff' }}
                title={
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Space>
                            <div style={{ width: 10, height: 10, borderRadius: '50%', background: '#1890ff' }}></div>
                            <Text strong style={{ color: '#003a8c' }}>TÀI LIỆU ĐỐI SOÁT CHI TIẾT</Text>
                        </Space>
                        <Button 
                            type="primary" 
                            danger 
                            ghost 
                            size="small" 
                            icon={<CloseOutlined />} 
                            onClick={onClose}
                        >
                            Đóng
                        </Button>
                    </div>
                }
                headStyle={{ background: '#f0f7ff', borderBottom: '1px solid #bae7ff' }}
                bodyStyle={{ padding: 16 }}
            >
                <div style={{ marginBottom: 16 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', gap: 15, background: '#fff', padding: 12, borderRadius: 8, border: '1px solid #e6f7ff' }}>
                        <div style={{ flex: 1, minWidth: 0 }}>
                            <Title level={5} style={{ color: '#003a8c', marginBottom: 4, fontSize: 14, wordBreak: 'break-word' }}>{match.source}</Title>
                            <Space split={<Divider type="vertical" />}>
                                {match.author && <Text type="secondary" style={{ fontSize: 11 }}><UserOutlined /> {match.author}</Text>}
                                <Tag color="blue" icon={<FileSearchOutlined />} style={{ fontSize: 10 }}>Đang so khớp</Tag>
                            </Space>
                        </div>
                        <div style={{ textAlign: 'right', flexShrink: 0 }}>
                            <Statistic title="Góp mặt" value={match.similarity} suffix="%" valueStyle={{ color: '#1890ff', fontSize: 20, fontWeight: 'bold' }} />
                        </div>
                    </div>
                </div>
                
                <div 
                    className="custom-scrollbar" 
                    ref={contentRef} 
                    style={{ 
                        height: 'calc(100vh - 400px)', 
                        minHeight: 400, 
                        padding: '24px', 
                        background: '#f8fafc', 
                        overflowY: 'auto', 
                        overflowX: 'hidden',
                        border: '1px solid #e2e8f0', 
                        borderRadius: 8 
                    }}
                >
                    <div style={{ lineHeight: '1.8', whiteSpace: 'pre-wrap', wordBreak: 'break-word', overflowWrap: 'break-word', fontFamily: '"Merriweather", serif', fontSize: 15, color: '#334155', textAlign: 'justify' }}>
                        {loading ? (
                            <div style={{ textAlign: 'center', padding: '100px 0' }}>
                                <Spin tip="Đang tải dữ liệu nguồn..." />
                            </div>
                        ) : renderHighlightedSource(sourceContent || "")}
                    </div>
                </div>

                <div style={{ marginTop: 16, display: 'flex', gap: 8 }}>
                    <Tag color="blue" style={{ flex: 1, textAlign: 'center', padding: '4px 0' }}>Đang chọn</Tag>
                    <Tag color="orange" style={{ flex: 1, textAlign: 'center', padding: '4px 0' }}>Trùng lặp khác</Tag>
                </div>
            </Card>
        );
    };

    const TurnitinSidebar = () => {
        if (currentStep !== 2 || !filteredResult) return null;

        const activeScore = Math.round(filteredResult.score);
        
        return (
            <div className="turnitin-sidebar-container">
                <style>{`
                    .turnitin-sidebar-container {
                        position: fixed;
                        top: 64px;
                        right: 0;
                        height: calc(100vh - 64px);
                        display: flex;
                        z-index: 1001;
                    }
                    .turnitin-panel {
                        background: #fdfdfd;
                        width: ${sidePanelVisible ? '340px' : '0'};
                        overflow: hidden;
                        transition: width 0.3s cubic-bezier(0.2, 0, 0, 1);
                        box-shadow: -10px 0 20px rgba(0,0,0,0.08);
                        border-left: 1px solid #e0e0e0;
                        display: flex;
                        flex-direction: column;
                    }
                    .turnitin-vertical-tabs {
                        width: 55px;
                        background: #333639;
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        padding-top: 15px;
                        box-shadow: -2px 0 5px rgba(0,0,0,0.2);
                    }
                    .v-tab-item {
                        width: 55px;
                        height: 70px;
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        justify-content: center;
                        cursor: pointer;
                        color: #adb5bd;
                        transition: all 0.2s;
                        border-bottom: 1px solid #444;
                        position: relative;
                        padding: 10px 0;
                    }
                    .v-tab-item:hover {
                        background: #444;
                        color: #fff;
                    }
                    .v-tab-item.active {
                        background: #1890ff; /* Primary Blue */
                        color: #fff;
                    }
                    .v-tab-item .tab-val {
                        font-weight: 800;
                        font-size: 18px;
                        line-height: 1;
                        margin-bottom: 4px;
                    }
                    .v-tab-item .tab-icon {
                        font-size: 18px;
                    }
                    .red-header {
                        background: #1890ff;
                        color: white;
                        padding: 18px 20px;
                        font-size: 16px;
                        font-weight: 700;
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    }
                    .panel-inner {
                        flex: 1;
                        overflow-y: auto;
                        padding: 24px;
                        background: #fff;
                    }
                    .filter-group {
                        margin-bottom: 32px;
                    }
                    .filter-label {
                        font-weight: 700;
                        color: #333;
                        font-size: 14px;
                        margin-bottom: 16px;
                        display: block;
                    }
                    .filter-row {
                        margin-bottom: 12px;
                        display: flex;
                        align-items: center;
                    }
                    .filter-row .ant-checkbox-wrapper {
                        font-size: 14px;
                        color: #555;
                    }
                    .sub-label {
                        font-size: 13px;
                        color: #666;
                        margin-bottom: 12px;
                        font-weight: 600;
                    }
                    .apply-btn {
                        background: #1890ff;
                        border-color: #1890ff;
                        height: 40px;
                        font-weight: 700;
                        text-transform: uppercase;
                        letter-spacing: 0.5px;
                    }
                    .apply-btn:hover {
                        background: #40a9ff !important;
                        border-color: #40a9ff !important;
                    }
                `}</style>
                
                <div className="turnitin-panel">
                    {sidePanelType === 'filters' && (
                        <>
                            <div className="red-header">
                                <span>Bộ lọc và Tùy chọn</span>
                                <CloseOutlined onClick={() => setSidePanelVisible(false)} style={{ cursor: 'pointer', fontSize: 14 }} />
                            </div>
                            <div className="panel-inner custom-scrollbar">
                                <div className="filter-group">
                                    <span className="filter-label">BỘ LỌC</span>
                                    <div className="filter-row">
                                        <Checkbox 
                                            checked={excludeQuotes} 
                                            onChange={e => setExcludeQuotes(e.target.checked)}
                                        >
                                            Loại trừ Trích dẫn
                                        </Checkbox>
                                    </div>
                                    <div className="filter-row">
                                        <Checkbox 
                                            checked={excludeBibliography} 
                                            onChange={e => setExcludeBibliography(e.target.checked)}
                                        >
                                            Loại trừ Mục lục Tham khảo
                                        </Checkbox>
                                    </div>
                                </div>

                                <Divider style={{ margin: '16px 0 24px' }} />

                                <div className="filter-group">
                                    <span className="filter-label">LOẠI TRỪ CÁC NGUỒN CÓ ÍT HƠN:</span>
                                    <Radio.Group 
                                        value={excludeType} 
                                        onChange={e => setExcludeType(e.target.value)}
                                        style={{ width: '100%' }}
                                    >
                                        <Space direction="vertical" size={16} style={{ width: '100%' }}>
                                            <Radio value="words" style={{ display: 'flex', alignItems: 'center' }}>
                                                <Space>
                                                    <InputNumber 
                                                        size="small" 
                                                        min={1} 
                                                        value={excludeMinWords} 
                                                        onChange={val => setExcludeMinWords(val || 1)} 
                                                        disabled={excludeType !== 'words'}
                                                        style={{ width: 60 }}
                                                    />
                                                    <span>từ</span>
                                                </Space>
                                            </Radio>
                                            <Radio value="percent" style={{ display: 'flex', alignItems: 'center' }}>
                                                <Space>
                                                    <InputNumber 
                                                        size="small" 
                                                        min={1} 
                                                        max={100} 
                                                        value={excludeMinPercent} 
                                                        onChange={val => setExcludeMinPercent(val || 1)} 
                                                        disabled={excludeType !== 'percent'}
                                                        style={{ width: 60 }}
                                                    />
                                                    <span>%</span>
                                                </Space>
                                            </Radio>
                                            <Radio value="none">Không loại trừ theo kích thước</Radio>
                                        </Space>
                                    </Radio.Group>
                                </div>

                                <div style={{ marginTop: 40, textAlign: 'center' }}>
                                    <Button 
                                        type="primary" 
                                        className="apply-btn"
                                        block
                                        onClick={() => setSidePanelVisible(false)}
                                    >
                                        Áp dụng các thay đổi
                                    </Button>
                                    <Button 
                                        type="link" 
                                        style={{ marginTop: 12, color: '#666' }}
                                        onClick={() => {
                                            setExcludeQuotes(false);
                                            setExcludeBibliography(false);
                                            setExcludeType('none');
                                        }}
                                    >
                                        Thiết lập lại đầu
                                    </Button>
                                </div>
                            </div>
                        </>
                    )}

                    {sidePanelType === 'matches' && (
                        <>
                            <div className="red-header">
                                <span>Tóm tắt kết quả khớp</span>
                                <CloseOutlined onClick={() => setSidePanelVisible(false)} style={{ cursor: 'pointer', fontSize: 14 }} />
                            </div>
                            <div className="panel-inner custom-scrollbar" style={{ padding: '16px' }}>
                                <div style={{ marginBottom: 20, textAlign: 'center' }}>
                                    <Statistic 
                                        value={activeScore} 
                                        suffix="%" 
                                        title={<Text strong style={{ color: '#1890ff' }}>CHỈ SỐ TRÙNG KHỚP</Text>}
                                        valueStyle={{ color: '#1890ff', fontSize: 36, fontWeight: 900 }}
                                    />
                                </div>
                                <Divider style={{ margin: '12px 0 20px' }} />
                                {filteredResult.matches.map((m: any, idx: number) => (
                                    <div 
                                        key={idx} 
                                        className="simple-match-row"
                                        style={{
                                            borderLeft: `4px solid ${m.severity === 'high' ? '#ff4d4f' : (m.severity === 'medium' ? '#faad14' : '#52c41a')}`,
                                            background: selectedMatch?.source === m.source ? '#fff1f0' : '#fff',
                                            padding: '12px',
                                            marginBottom: '8px',
                                            borderRadius: '0 4px 4px 0',
                                            cursor: 'pointer',
                                            boxShadow: '0 1px 3px rgba(0,0,0,0.05)',
                                            border: selectedMatch?.source === m.source ? '1px solid #ffccc7' : '1px solid #f0f0f0',
                                            borderLeftWidth: '4px'
                                        }}
                                        onClick={() => {
                                            const segments = (result?.detailedAnalysis?.segments || []);
                                            const allMatchedTexts = segments
                                                .filter((s: any) => s.source === m.source && s.matchedText)
                                                .map((s: any) => s.matchedText);
                                            const firstSeg = segments.find((s: any) => s.source === m.source && s.matchedText);
                                            
                                            setSelectedMatch({ 
                                                ...m, 
                                                text: firstSeg?.matchedText || m.text,
                                                allSnippets: allMatchedTexts 
                                            });
                                            const matchIndex = segments.findIndex((s: any) => s.source === m.source);
                                            if (matchIndex !== -1) {
                                                setActiveMatchId(matchIndex);
                                                document.getElementById(`match-${matchIndex}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
                                            }
                                        }}
                                    >
                                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                                            <Text strong style={{ color: '#1890ff', fontSize: 16 }}>{idx + 1}</Text>
                                            <Text strong style={{ color: '#333' }}>{m.similarity}%</Text>
                                        </div>
                                        <Text ellipsis style={{ fontSize: 12, display: 'block', color: '#555' }}>{m.source}</Text>
                                    </div>
                                ))}
                            </div>
                        </>
                    )}

                    {sidePanelType === 'info' && (
                        <>
                            <div className="red-header" style={{ background: '#595959' }}>
                                <span>Thông tin bài nộp</span>
                                <CloseOutlined onClick={() => setSidePanelVisible(false)} style={{ cursor: 'pointer', fontSize: 14 }} />
                            </div>
                            <div className="panel-inner custom-scrollbar" style={{ padding: '24px 16px' }}>
                                <div style={{ marginBottom: 32 }}>
                                    <Text strong style={{ color: '#8c8c8c', textTransform: 'uppercase', fontSize: 12, display: 'block', marginBottom: 12 }}>
                                        CHI TIẾT BÀI NỘP
                                    </Text>
                                    <Space direction="vertical" size={16} style={{ width: '100%' }}>
                                        <div style={{ paddingBottom: 12, borderBottom: '1px solid #f0f0f0' }}>
                                            <Text type="secondary" style={{ fontSize: 12, display: 'block' }}>Tên tệp:</Text>
                                            <Text strong style={{ fontSize: 14, color: '#262626' }}>{pendingFileName || "Tài liệu không tên"}</Text>
                                        </div>
                                        <div style={{ paddingBottom: 12, borderBottom: '1px solid #f0f0f0' }}>
                                            <Text type="secondary" style={{ fontSize: 12, display: 'block' }}>ID Bài nộp:</Text>
                                            <Text copyable style={{ fontSize: 14, color: '#262626' }}>{sourceDocId || "BAV-" + new Date().getTime()}</Text>
                                        </div>
                                        <div style={{ paddingBottom: 12, borderBottom: '1px solid #f0f0f0' }}>
                                            <Text type="secondary" style={{ fontSize: 12, display: 'block' }}>Ngày kiểm tra:</Text>
                                            <Text style={{ fontSize: 14, color: '#262626' }}>{checkInfo?.checkDate ? new Date(checkInfo.checkDate).toLocaleString('vi-VN') : new Date().toLocaleString('vi-VN')}</Text>
                                        </div>
                                        <div style={{ paddingBottom: 12 }}>
                                            <Text type="secondary" style={{ fontSize: 12, display: 'block' }}>Người nộp:</Text>
                                            <Text style={{ fontSize: 14, color: '#262626' }}>{checkInfo?.userName || "N/A"}</Text>
                                        </div>
                                    </Space>
                                </div>

                                <Divider style={{ margin: '24px 0' }} />

                                <div>
                                    <Text strong style={{ color: '#8c8c8c', textTransform: 'uppercase', fontSize: 12, display: 'block', marginBottom: 12 }}>
                                        THÔNG SỐ VĂN BẢN
                                    </Text>
                                    <Space direction="vertical" size={16} style={{ width: '100%' }}>
                                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                            <Text type="secondary">Số lượng từ:</Text>
                                            <Text strong>{fullText ? fullText.trim().split(/\s+/).length : 0}</Text>
                                        </div>
                                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                            <Text type="secondary">Số lượng ký tự:</Text>
                                            <Text strong>{fullText?.length || 0}</Text>
                                        </div>
                                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                            <Text type="secondary">Số lượng đoạn văn:</Text>
                                            <Text strong>{result?.detailedAnalysis?.segments?.length || 0}</Text>
                                        </div>
                                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                            <Text type="secondary">Chỉ số trùng khớp:</Text>
                                            <Tag color="#1890ff" style={{ margin: 0 }}>{result?.score?.toFixed(1) || 0}%</Tag>
                                        </div>
                                    </Space>
                                </div>
                            </div>
                        </>
                    )}
                </div>

                <div className="turnitin-vertical-tabs">
                    <div 
                        className={`v-tab-item ${sidePanelType === 'matches' && sidePanelVisible ? 'active' : ''}`}
                        onClick={() => {
                            if (sidePanelType === 'matches' && sidePanelVisible) {
                                setSidePanelVisible(false);
                            } else {
                                setSidePanelVisible(true);
                                setSidePanelType('matches');
                            }
                        }}
                    >
                        <div className="tab-val">{activeScore}</div>
                        <LayersOutlined className="tab-icon" />
                    </div>
                    <div 
                        className={`v-tab-item ${sidePanelType === 'filters' && sidePanelVisible ? 'active' : ''}`}
                        onClick={() => {
                            if (sidePanelType === 'filters' && sidePanelVisible) {
                                setSidePanelVisible(false);
                            } else {
                                setSidePanelVisible(true);
                                setSidePanelType('filters');
                            }
                        }}
                    >
                        <FilterOutlined style={{ fontSize: 22 }} />
                    </div>
                    <div className="v-tab-item" onClick={() => window.print()}>
                        <DownloadOutlined style={{ fontSize: 22 }} />
                    </div>
                    <div 
                        className={`v-tab-item ${sidePanelType === 'info' && sidePanelVisible ? 'active' : ''}`}
                        onClick={() => {
                            if (sidePanelType === 'info' && sidePanelVisible) {
                                setSidePanelVisible(false);
                            } else {
                                setSidePanelVisible(true);
                                setSidePanelType('info');
                            }
                        }}
                    >
                        <InfoCircleOutlined style={{ fontSize: 22 }} />
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
        const currentData = filteredResult || result;
        if (!currentData?.detailedAnalysis?.segments) return <Paragraph>{fullText}</Paragraph>;

        return currentData.detailedAnalysis.segments.map((seg: any, idx: number) => {
            if (seg.isExcluded) {
                return <span key={idx}>{seg.text}</span>;
            }

            if (seg.score > 0) {
                // Only highlight if the source is still in our filtered matches list
                const isActiveSource = currentData.matches.some((m: any) => m.source === seg.source);
                
                if (!isActiveSource) return <span key={idx}>{seg.text}</span>;

                const isSelected = selectedMatch && selectedMatch.source === seg.source;
                const className = `highlight-${seg.severity} ${isSelected ? 'highlight-active' : ''}`;
                return (
                    <span
                        key={idx}
                        className={className}
                        onClick={() => {
                            const match = currentData.matches.find((m: any) => m.source === seg.source);
                            if (match) {
                                // CRITICAL: pass seg.matchedText (text from SOURCE document),
                                // NOT seg.text (text from submitted document).
                                // Also collect all matchedText snippets from other segments of the same source
                                const allMatchedTexts = currentData.detailedAnalysis.segments
                                    .filter((s: any) => s.source === seg.source && s.matchedText)
                                    .map((s: any) => s.matchedText);
                                
                                setSelectedMatch({ 
                                    ...match, 
                                    text: seg.matchedText || seg.text,
                                    allSnippets: allMatchedTexts 
                                });
                                setActiveMatchId(seg.id);
                            }
                        }}
                        id={`match-${idx}`}
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
                                        <Badge status="processing" text="Dữ liệu nội bộ BAV" />
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
                                percent={Math.round(progress)}
                                format={(percent) => `${Math.round(percent || 0)}%`}
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

                    {currentStep === 2 && (result || filteredResult) && (
                        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ duration: 0.5 }}>
                            <div style={{ paddingRight: sidePanelVisible ? 395 : 55, transition: 'padding 0.3s ease' }}>
                                <TurnitinSidebar />
                                
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
                                                {/* Left side - Statistics using FILTERED results */}
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
                                                                    {(filteredResult?.score ?? result.score).toFixed(1)}%
                                                                </div>
                                                                <div style={{ color: 'rgba(255,255,255,0.9)', fontSize: 16, marginTop: 8 }}>
                                                                    Chỉ số trùng khớp
                                                                </div>
                                                                <Tag
                                                                    color={(filteredResult?.score ?? result.score) > 20 ? 'error' : 'success'}
                                                                    style={{ marginTop: 12, fontSize: 13, padding: '4px 12px' }}
                                                                >
                                                                    {(filteredResult?.score ?? result.score) > 20 ? '⚠️ Nguy cơ cao' : '✅ An toàn'}
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
                                                                    {filteredResult?.matchedDocs ?? result.matchedDocs}
                                                                </div>
                                                                <div style={{ color: 'rgba(255,255,255,0.9)', fontSize: 16, marginTop: 8 }}>
                                                                    Nguồn trùng khớp
                                                                </div>
                                                                <Tag
                                                                    color="processing"
                                                                    style={{ marginTop: 12, fontSize: 13, padding: '4px 12px' }}
                                                                >
                                                                    📚 {filteredResult?.matchedDocs ?? result.matchedDocs} tài liệu
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
                                                                Phân tích chất lượng
                                                            </Button>
                                                        )}
                                                        <Button
                                                            block
                                                            size="large"
                                                            onClick={() => setSidePanelVisible(true)}
                                                            style={{ fontWeight: 600, background: '#1890ff', color: '#fff', border: 'none' }}
                                                        >
                                                            ⚙️ Mở Bộ lọc nâng cao
                                                        </Button>
                                                        <Button
                                                            block
                                                            size="large"
                                                            ghost
                                                            icon={<ArrowLeftOutlined />}
                                                            onClick={resetAnalysis}
                                                            style={{ 
                                                                color: '#fff', 
                                                                borderColor: 'rgba(255,255,255,0.6)',
                                                                fontWeight: 600,
                                                                marginTop: 12
                                                            }}
                                                        >
                                                            Kiểm tra tài liệu khác
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
                                    <Col xs={24} lg={selectedMatch ? 12 : 16}>
                                        <Card
                                            title={
                                                <Space>
                                                    <FileTextOutlined style={{ color: '#1890ff' }} />
                                                    <Text strong style={{ color: '#003a8c' }}>{selectedMatch ? "BẢN GỐC (BÀI NỘP)" : "Nội dung bài nộp"}</Text>
                                                </Space>
                                            }
                                            size="small"
                                            className="glass-card"
                                            headStyle={{ background: '#f8fafc', borderBottom: '1px solid #e2e8f0', padding: '12px 20px' }}
                                            bodyStyle={{ padding: 0 }}
                                            style={{ height: '100%' }}
                                        >
                                            <div className="plagiarism-word-container custom-scrollbar" style={{ height: 'calc(100vh - 350px)', minHeight: 450, padding: '24px', background: '#fff', overflowX: 'hidden' }}>
                                                <div className="word-page" style={{ lineHeight: '1.8', fontSize: 15, whiteSpace: 'pre-wrap', color: '#334155' }}>
                                                    {renderDetailedAnalysis()}
                                                </div>
                                            </div>
                                        </Card>
                                    </Col>

                                    <Col xs={24} lg={selectedMatch ? 12 : 8}>
                                        {selectedMatch ? (
                                            renderComparisonSource()
                                        ) : (
                                            <div className="matches-summary-container">
                                                <Card
                                                    title={<Space><LayersOutlined style={{ color: '#1890ff' }} /> <Text strong>Tổng hợp nguồn trùng khớp</Text></Space>}
                                                    className="glass-card"
                                                    bodyStyle={{ padding: '0px' }}
                                                >
                                                    <div className="custom-scrollbar" style={{ maxHeight: 'calc(100vh - 425px)', overflowY: 'auto', padding: '12px' }}>
                                                        {(filteredResult || result).matches.map((item: any, index: number) => {
                                                            const isSelected = !!selectedMatch && (selectedMatch as any).source === item.source;
                                                            return (
                                                                <div
                                                                    key={index}
                                                                    className={`match-item-turnitin ${isSelected ? 'active' : ''}`}
                                                                    style={{
                                                                        padding: '16px',
                                                                        borderRadius: '8px',
                                                                        marginBottom: '12px',
                                                                        cursor: 'pointer',
                                                                        transition: 'all 0.2s',
                                                                        border: isSelected ? '2px solid #1890ff' : '1px solid #eee',
                                                                        background: isSelected ? '#e6f7ff' : '#fff',
                                                                        borderLeftWidth: '8px',
                                                                        borderLeftColor: item.similarity > 50 ? '#ff4d4f' : (item.similarity > 20 ? '#faad14' : '#52c41a')
                                                                    }}
                                                                    onClick={() => {
                                                                        const segments = (filteredResult || result)?.detailedAnalysis?.segments || [];
                                                                        // Collect all matchedText snippets from segments matching this source
                                                                        const allMatchedTexts = segments
                                                                            .filter((s: any) => s.source === item.source && s.matchedText)
                                                                            .map((s: any) => s.matchedText);
                                                                        
                                                                        const firstSeg = segments.find((s: any) => s.source === item.source && s.matchedText);
                                                                        
                                                                        setSelectedMatch({ 
                                                                            ...item, 
                                                                            text: firstSeg?.matchedText || item.text,
                                                                            allSnippets: allMatchedTexts 
                                                                        });
                                                                        const matchIndex = segments.findIndex((s: any) => s.source === item.source);
                                                                        if (matchIndex !== -1) {
                                                                            setActiveMatchId(matchIndex);
                                                                            document.getElementById(`match-${matchIndex}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
                                                                        }
                                                                    }}
                                                                >
                                                                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 }}>
                                                                        <Text strong style={{ color: '#1890ff', fontSize: 16 }}>{index + 1}</Text>
                                                                        <Text strong style={{ fontSize: 18, color: '#333' }}>{item.similarity.toFixed(1)}%</Text>
                                                                    </div>
                                                                    <Text ellipsis={true} style={{ display: 'block', fontSize: 14, fontWeight: 600, color: '#444', marginBottom: 8 }}>
                                                                        {item.source}
                                                                    </Text>
                                                                    <Progress 
                                                                        percent={item.similarity} 
                                                                        size="small" 
                                                                        showInfo={false} 
                                                                        strokeColor={item.similarity > 50 ? '#ca2027' : (item.similarity > 20 ? '#faad14' : '#52c41a')} 
                                                                    />
                                                                    <div style={{ marginTop: 8, display: 'flex', justifyContent: 'space-between' }}>
                                                                        <Tag color="default" style={{ fontSize: 10 }}>BAV Database</Tag>
                                                                        {item.author && <Text type="secondary" style={{ fontSize: 11 }}>{item.author}</Text>}
                                                                    </div>
                                                                </div>
                                                            );
                                                        })}
                                                    </div>
                                                </Card>
                                            </div>
                                        )}
                                    </Col>
                                </Row>
                            </div>
                        </motion.div>
                    )}
                </Card>

                <Card className="glass-card" title={<Space><InfoCircleOutlined style={{ color: '#faad14' }} /> <Text>Quy định về đạo văn tại BAV</Text></Space>}>
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
            {/* --- Printable Certificate --- */}
            <div className="print-only" style={{ padding: '40px', fontFamily: '"Times New Roman", Times, serif', width: '100%', height: '100vh', background: 'white' }}>
                <div style={{ textAlign: 'center', marginBottom: 40 }}>
                    <h2 style={{ margin: 0, fontSize: 24, textTransform: 'uppercase' }}>HỌC VIỆN NGÂN HÀNG</h2>
                    <h3 style={{ margin: 0, fontSize: 18, fontWeight: 'normal' }}>Trung tâm Nghiên cứu Đạo văn (BAV Plagiarism System)</h3>
                    <div style={{ width: 100, height: 2, background: 'black', margin: '20px auto' }}></div>
                </div>
                
                <div style={{ textAlign: 'center', marginBottom: 50 }}>
                    <h1 style={{ fontSize: 32, textTransform: 'uppercase', color: '#1a1a1a', fontWeight: 'bold' }}>Giấy Chứng Nhận Tính Nguyên Bản</h1>
                    <p style={{ fontStyle: 'italic', fontSize: 16 }}>Tài liệu kèm theo Báo cáo / Đồ án / Khóa luận tốt nghiệp</p>
                </div>

                <div style={{ fontSize: 16, lineHeight: 2, padding: '0 40px' }}>
                    <p><strong>Tên tài liệu:</strong> {checkInfo?.fileName || pendingFileName}</p>
                    <p><strong>Người nộp:</strong> {checkInfo?.userName || user?.fullName}</p>
                    <p><strong>Ngày kiểm tra:</strong> {checkInfo ? new Date(checkInfo.checkDate).toLocaleDateString('vi-VN') : new Date().toLocaleDateString('vi-VN')}</p>
                    <p><strong>Ngày xuất báo cáo:</strong> {new Date().toLocaleDateString('vi-VN')} lúc {new Date().toLocaleTimeString('vi-VN')}</p>
                    
                    <div style={{ margin: '40px 0', padding: '20px', border: '2px dashed #000', textAlign: 'center', borderRadius: 8 }}>
                        <p style={{ fontSize: 20, margin: 0 }}>Kết quả tỷ lệ trùng lặp trên kho dữ liệu của trường lớn:</p>
                        <p style={{ fontSize: 48, fontWeight: 'bold', margin: '10px 0' }}>
                            {(filteredResult?.score ?? result?.score ?? 0).toFixed(1)}%
                        </p>
                        <p style={{ fontSize: 18, margin: 0 }}>
                            Đánh giá: <strong>{(filteredResult?.score ?? result?.score) > 20 ? 'CHƯA ĐẠT (Cần sửa chữa lại do vượt mức 20% cho phép)' : 'ĐẠT TIÊU CHUẨN'}</strong>
                        </p>
                    </div>

                    <p><strong>Tóm tắt nhận định AI:</strong> văn bản có xác suất do AI tạo ra là {result?.aiProbability}%.</p>
                </div>

                <div style={{ marginTop: 100, display: 'flex', justifyContent: 'space-between', padding: '0 80px' }}>
                    <div style={{ textAlign: 'center' }}>
                        <p><strong>Sinh viên nộp bài</strong></p>
                        <p style={{ fontStyle: 'italic', fontSize: 14 }}>(Ký và ghi rõ họ tên)</p>
                    </div>
                    <div style={{ textAlign: 'center' }}>
                        <p><strong>Hệ thống BAV Plagiarism</strong></p>
                        <p style={{ fontStyle: 'italic', fontSize: 14 }}>Xác nhận điện tử</p>
                    </div>
                </div>
            </div>
            {/* --- End Printable Certificate --- */}
        </>
    );
};

export default PlagiarismCheckPage;
