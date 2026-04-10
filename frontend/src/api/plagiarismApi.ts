import axiosClient from './axiosClient';

export interface CreatePlagiarismCheckDto {
    sourceDocumentId: number;
    notes?: string;
}

export interface PlagiarismCheckResultDto {
    checkId: number;
    overallSimilarityPercentage: number;
    totalMatchedDocuments: number;
    matchDetails: any[];
    detailedAnalysis: {
        overallScore: number;
        segments: any[];
    };
    status: string;
    checkDate: string;
    aiProbability?: number;
    aiDetectionLevel?: string;
    aiAnalysis?: {
        aiProbability: number;
        detectionLevel: string;
        sentences: any[];
        summary: string;
    };
    remainingChecksToday: number;
    dailyCheckLimit: number;
}

export interface PlagiarismStatisticsDto {
    totalChecks: number;
    totalDocuments: number;
    totalUsers: number;
    totalFaculties: number;
    totalSubjects: number;
    totalStudents: number;
    averageSimilarity: number;
    highRiskCount: number;
    mediumRiskCount: number;
    lowRiskCount: number;
    subjectStats: any[];
}

const plagiarismApi = {
    // Thực hiện kiểm tra đạo văn cho tài liệu
    check: (data: CreatePlagiarismCheckDto): Promise<PlagiarismCheckResultDto> => {
        return axiosClient.post('/plagiarism/check', data);
    },
    // Lấy lịch sử kiểm tra của người dùng
    getHistory: (params?: any): Promise<any[]> => {
        return axiosClient.get('/plagiarism/history', { params });
    },
    // Lấy chi tiết kết quả kiểm tra theo ID
    getDetail: (id: number): Promise<any> => {
        return axiosClient.get(`/plagiarism/checks/${id}`);
    },
    // Lấy thống kê tổng quan về đạo văn
    getStatistics: (params?: any): Promise<PlagiarismStatisticsDto> => {
        return axiosClient.get('/plagiarism/statistics', { params });
    },
    // Lấy danh sách các tài liệu có độ trùng lặp cao
    getHighRisk: (threshold: number = 50, limit: number = 10): Promise<any[]> => {
        return axiosClient.get('/plagiarism/high-risk', { params: { threshold, limit } });
    }
};

export default plagiarismApi;
