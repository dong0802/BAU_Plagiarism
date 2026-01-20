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
    remainingChecksToday: number;
    dailyCheckLimit: number;
}

export interface PlagiarismStatisticsDto {
    totalChecks: number;
    totalDocuments: number;
    totalUsers: number;
    averageSimilarity: number;
    highRiskCount: number;
    mediumRiskCount: number;
    lowRiskCount: number;
    subjectStats: any[];
}

const plagiarismApi = {
    check: (data: CreatePlagiarismCheckDto): Promise<PlagiarismCheckResultDto> => {
        return axiosClient.post('/plagiarism/check', data);
    },
    getHistory: (params?: any): Promise<any[]> => {
        return axiosClient.get('/plagiarism/history', { params });
    },
    getDetail: (id: number): Promise<any> => {
        return axiosClient.get(`/plagiarism/checks/${id}`);
    },
    getStatistics: (params?: any): Promise<PlagiarismStatisticsDto> => {
        return axiosClient.get('/plagiarism/statistics', { params });
    },
    getHighRisk: (threshold: number = 50, limit: number = 10): Promise<any[]> => {
        return axiosClient.get('/plagiarism/high-risk', { params: { threshold, limit } });
    }
};

export default plagiarismApi;
