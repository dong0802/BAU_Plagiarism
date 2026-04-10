import axiosClient from './axiosClient';

export interface DocumentQualityAnalysis {
    documentId: number;
    overallQualityScore: number;
    qualityLevel: string;
    formatAnalysis: FormatAnalysis;
    contentQuality: ContentQuality;
    issues: QualityIssue[];
    suggestions: string[];
    analyzedDate: string;
}

export interface FormatAnalysis {
    hasProperStructure: boolean;
    wordCount: number;
    paragraphCount: number;
    sentenceCount: number;
    averageSentenceLength: number;
    averageParagraphLength: number;
    hasTitle: boolean;
    hasIntroduction: boolean;
    hasConclusion: boolean;
    hasReferences: boolean;
    formatScore: number;
}

export interface ContentQuality {
    readabilityScore: number;
    coherenceScore: number;
    vocabularyRichness: number;
    uniqueWords: number;
    totalWords: number;
    lexicalDiversity: number;
    keyPhrases: string[];
    academicTerms: string[];
    contentScore: number;
}

export interface QualityIssue {
    issueType: string;
    severity: string;
    description: string;
    suggestion: string;
    position: number;
}

export interface WebSearchResult {
    query: string;
    sources: WebSource[];
    totalMatches: number;
    searchDate: string;
}

export interface WebSource {
    title: string;
    url: string;
    snippet: string;
    similarityScore: number;
    sourceType: string;
}

const qualityApi = {
    // Phân tích chất lượng tài liệu đã lưu bằng ID
    analyzeDocument: (documentId: number): Promise<DocumentQualityAnalysis> => {
        return axiosClient.post(`/documentquality/analyze/${documentId}`);
    },
    // Phân tích trực tiếp dữ liệu văn bản (không lưu)
    analyzeText: (content: string, title?: string): Promise<DocumentQualityAnalysis> => {
        return axiosClient.post('/documentquality/analyze-text', { content, title });
    }
};

export default qualityApi;
