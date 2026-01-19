import axiosClient from './axiosClient';

export interface DocumentUploadDto {
    title: string;
    documentType: string;
    subjectId?: number;
    semester?: string;
    year?: number;
    isPublic: boolean;
    file: File;
}

export interface DocumentDto {
    id: number;
    title: string;
    documentType: string;
    originalFileName: string;
    fileSize: number;
    userId: number;
    userName: string;
    subjectId?: number;
    subjectName?: string;
    semester?: string;
    year?: number;
    uploadDate: string;
    isPublic: boolean;
    isActive: boolean;
}

const documentApi = {
    getAll: (params?: any): Promise<DocumentDto[]> => {
        return axiosClient.get('/documents', { params });
    },
    getById: (id: number): Promise<DocumentDto> => {
        return axiosClient.get(`/documents/${id}`);
    },
    upload: (data: DocumentUploadDto): Promise<DocumentDto> => {
        const formData = new FormData();
        formData.append('file', data.file);
        formData.append('title', data.title);
        formData.append('documentType', data.documentType);
        if (data.subjectId) formData.append('subjectId', data.subjectId.toString());
        if (data.semester) formData.append('semester', data.semester);
        if (data.year) formData.append('year', data.year.toString());
        formData.append('isPublic', data.isPublic.toString());

        return axiosClient.post('/documents/upload', formData, {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
        });
    },
    update: (id: number, data: any): Promise<DocumentDto> => {
        return axiosClient.put(`/documents/${id}`, data);
    },
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/documents/${id}`);
    },
    getContent: (id: number): Promise<{ content: string }> => {
        return axiosClient.get(`/documents/${id}/content`);
    }
};

export default documentApi;
