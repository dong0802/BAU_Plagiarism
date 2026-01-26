import React from 'react';
import { Modal, Row, Col, Card, Statistic, Tag, Divider, Space, Typography, Progress, List, Badge, Button } from 'antd';
import { CheckCircleOutlined, ClockCircleOutlined } from '@ant-design/icons';
import { DocumentQualityAnalysis } from '../api/qualityApi';

const { Text } = Typography;

interface QualityModalProps {
    visible: boolean;
    onClose: () => void;
    analysis: DocumentQualityAnalysis | null;
}

const QualityAnalysisModal: React.FC<QualityModalProps> = ({ visible, onClose, analysis }) => {
    if (!analysis) return null;

    const getScoreColor = (score: number) => {
        if (score >= 85) return '#52c41a';
        if (score >= 70) return '#1890ff';
        if (score >= 50) return '#faad14';
        return '#ff4d4f';
    };

    const getSeverityColor = (severity: string) => {
        if (severity === 'High') return 'error';
        if (severity === 'Medium') return 'warning';
        return 'default';
    };

    return (
        <Modal
            title={
                <Space>
                    <CheckCircleOutlined style={{ color: '#1890ff' }} />
                    <span>Phân tích Chất lượng Văn bản</span>
                </Space>
            }
            open={visible}
            onCancel={onClose}
            footer={[
                <Button key="close" type="primary" onClick={onClose}>Đóng</Button>
            ]}
            width={900}
        >
            {/* Overall Score */}
            <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
                <Col span={8}>
                    <Card size="small" style={{ textAlign: 'center', background: '#f0f5ff' }}>
                        <Statistic
                            title="Điểm Tổng Quan"
                            value={analysis.overallQualityScore.toFixed(1)}
                            suffix="/ 100"
                            valueStyle={{ color: getScoreColor(analysis.overallQualityScore), fontWeight: 'bold' }}
                        />
                        <Tag color={getScoreColor(analysis.overallQualityScore)} style={{ marginTop: 8 }}>
                            {analysis.qualityLevel}
                        </Tag>
                    </Card>
                </Col>
                <Col span={8}>
                    <Card size="small" style={{ textAlign: 'center', background: '#fff7e6' }}>
                        <Statistic
                            title="Điểm Định dạng"
                            value={analysis.formatAnalysis.formatScore}
                            suffix="/ 100"
                            valueStyle={{ color: getScoreColor(analysis.formatAnalysis.formatScore) }}
                        />
                    </Card>
                </Col>
                <Col span={8}>
                    <Card size="small" style={{ textAlign: 'center', background: '#f6ffed' }}>
                        <Statistic
                            title="Điểm Nội dung"
                            value={analysis.contentQuality.contentScore}
                            suffix="/ 100"
                            valueStyle={{ color: getScoreColor(analysis.contentQuality.contentScore) }}
                        />
                    </Card>
                </Col>
            </Row>

            {/* Format Analysis */}
            <Divider orientation="left">📋 Phân tích Cấu trúc</Divider>
            <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
                <Col span={6}>
                    <Text type="secondary">Số từ:</Text>
                    <div><Text strong>{analysis.formatAnalysis.wordCount}</Text></div>
                </Col>
                <Col span={6}>
                    <Text type="secondary">Số câu:</Text>
                    <div><Text strong>{analysis.formatAnalysis.sentenceCount}</Text></div>
                </Col>
                <Col span={6}>
                    <Text type="secondary">Số đoạn:</Text>
                    <div><Text strong>{analysis.formatAnalysis.paragraphCount}</Text></div>
                </Col>
                <Col span={6}>
                    <Text type="secondary">TB từ/câu:</Text>
                    <div><Text strong>{analysis.formatAnalysis.averageSentenceLength.toFixed(1)}</Text></div>
                </Col>
            </Row>

            <Space wrap style={{ marginBottom: 16 }}>
                <Tag icon={analysis.formatAnalysis.hasTitle ? <CheckCircleOutlined /> : <ClockCircleOutlined />}
                    color={analysis.formatAnalysis.hasTitle ? 'success' : 'default'}>
                    Tiêu đề
                </Tag>
                <Tag icon={analysis.formatAnalysis.hasIntroduction ? <CheckCircleOutlined /> : <ClockCircleOutlined />}
                    color={analysis.formatAnalysis.hasIntroduction ? 'success' : 'default'}>
                    Mở bài
                </Tag>
                <Tag icon={analysis.formatAnalysis.hasConclusion ? <CheckCircleOutlined /> : <ClockCircleOutlined />}
                    color={analysis.formatAnalysis.hasConclusion ? 'success' : 'default'}>
                    Kết luận
                </Tag>
                <Tag icon={analysis.formatAnalysis.hasReferences ? <CheckCircleOutlined /> : <ClockCircleOutlined />}
                    color={analysis.formatAnalysis.hasReferences ? 'success' : 'default'}>
                    Tài liệu tham khảo
                </Tag>
            </Space>

            {/* Content Quality */}
            <Divider orientation="left">📚 Chất lượng Nội dung</Divider>
            <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
                <Col span={8}>
                    <Progress
                        type="circle"
                        percent={Math.round(analysis.contentQuality.readabilityScore)}
                        width={80}
                        strokeColor={getScoreColor(analysis.contentQuality.readabilityScore)}
                    />
                    <div style={{ textAlign: 'center', marginTop: 8 }}>
                        <Text type="secondary">Độ dễ đọc</Text>
                    </div>
                </Col>
                <Col span={8}>
                    <Progress
                        type="circle"
                        percent={Math.round(analysis.contentQuality.coherenceScore)}
                        width={80}
                        strokeColor={getScoreColor(analysis.contentQuality.coherenceScore)}
                    />
                    <div style={{ textAlign: 'center', marginTop: 8 }}>
                        <Text type="secondary">Tính mạch lạc</Text>
                    </div>
                </Col>
                <Col span={8}>
                    <Progress
                        type="circle"
                        percent={Math.round(analysis.contentQuality.vocabularyRichness)}
                        width={80}
                        strokeColor={getScoreColor(analysis.contentQuality.vocabularyRichness)}
                    />
                    <div style={{ textAlign: 'center', marginTop: 8 }}>
                        <Text type="secondary">Vốn từ vựng</Text>
                    </div>
                </Col>
            </Row>

            {/* Issues */}
            {analysis.issues.length > 0 && (
                <>
                    <Divider orientation="left">⚠️ Vấn đề cần khắc phục</Divider>
                    <List
                        size="small"
                        dataSource={analysis.issues}
                        renderItem={(issue) => (
                            <List.Item>
                                <List.Item.Meta
                                    avatar={<Badge status={getSeverityColor(issue.severity) as any} />}
                                    title={
                                        <Space>
                                            <Tag color={getSeverityColor(issue.severity)}>{issue.issueType}</Tag>
                                            <Text>{issue.description}</Text>
                                        </Space>
                                    }
                                    description={<Text type="secondary">💡 {issue.suggestion}</Text>}
                                />
                            </List.Item>
                        )}
                    />
                </>
            )}

            {/* Suggestions */}
            {analysis.suggestions.length > 0 && (
                <>
                    <Divider orientation="left">💡 Gợi ý cải thiện</Divider>
                    <List
                        size="small"
                        dataSource={analysis.suggestions}
                        renderItem={(suggestion) => (
                            <List.Item>
                                <Text>{suggestion}</Text>
                            </List.Item>
                        )}
                    />
                </>
            )}
        </Modal>
    );
};

export default QualityAnalysisModal;
