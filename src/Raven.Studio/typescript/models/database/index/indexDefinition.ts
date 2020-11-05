/// <reference path="../../../../typings/tsd.d.ts"/>
import indexFieldOptions = require("models/database/index/indexFieldOptions");
import additionalSource = require("models/database/index/additionalSource");
import configurationItem = require("models/database/index/configurationItem");
import validateNameCommand = require("commands/resources/validateNameCommand");
import generalUtils = require("common/generalUtils");

class mapItem {
    map = ko.observable<string>();

    validationGroup: KnockoutObservable<any>;

    constructor(map: string) {
        this.map(map);

        this.initValidation();
    }

    private initValidation() {
        this.map.extend({
            required: true
        });

        this.validationGroup = ko.validatedObservable({
            map: this.map
        });
    }
}

class indexDefinition {
   
    name = ko.observable<string>();
    
    maps = ko.observableArray<mapItem>();
    reduce = ko.observable<string>();
    //isTestIndex = ko.observable<boolean>(false);
    
    fields = ko.observableArray<indexFieldOptions>();
    hasDuplicateFieldsNames: KnockoutComputed<boolean>;
    
    additionalSources = ko.observableArray<additionalSource>();
    defaultFieldOptions = ko.observable<indexFieldOptions>(null);
    isAutoIndex = ko.observable<boolean>(false);

    hasReduce = ko.observable<boolean>(false);
    outputReduceToCollection = ko.observable<boolean>();
    reduceOutputCollectionName = ko.observable<string>();
    
    createReferencesToResultsCollection = ko.observable<boolean>();
    patternForReferencesToReduceOutputCollection = ko.observable<string>();
    collectionNameForReferenceDocuments = ko.observable<string>();

    numberOfFields = ko.pureComputed(() => this.fields().length);
    numberOfConfigurationFields = ko.pureComputed(() => this.configuration() ? this.configuration().length : 0);

    configuration = ko.observableArray<configurationItem>();
    lockMode: Raven.Client.Documents.Indexes.IndexLockMode;

    priority = ko.observable<Raven.Client.Documents.Indexes.IndexPriority>();

    validationGroup: KnockoutValidationGroup;

    constructor(dto: Raven.Client.Documents.Indexes.IndexDefinition) {
        this.isAutoIndex(dto.Type.startsWith("Auto"));

        this.name(dto.Name);
        this.maps(dto.Maps.map(x => new mapItem(x)));
        this.reduce(dto.Reduce);
        this.hasReduce(!!dto.Reduce);
        //this.isTestIndex(dto.IsTestIndex);
        
        this.outputReduceToCollection(!!dto.OutputReduceToCollection);
        this.reduceOutputCollectionName(dto.OutputReduceToCollection);
        
        this.createReferencesToResultsCollection(!!dto.PatternForOutputReduceToCollectionReferences);
        this.patternForReferencesToReduceOutputCollection(dto.PatternForOutputReduceToCollectionReferences);
        this.collectionNameForReferenceDocuments(dto.PatternReferencesCollectionName);
        
        this.fields(_.map(dto.Fields, (fieldDto, indexName) => new indexFieldOptions(indexName, fieldDto, indexFieldOptions.defaultFieldOptions())));
        
        const defaultFieldOptions = this.fields().find(x => x.name() === indexFieldOptions.DefaultFieldOptions);
        if (defaultFieldOptions) {
            this.defaultFieldOptions(defaultFieldOptions);
            defaultFieldOptions.parent(indexFieldOptions.globalDefaults());
            this.fields.remove(defaultFieldOptions);

            this.fields().forEach(field => {
                field.parent(defaultFieldOptions);
            });
        }
        this.lockMode = dto.LockMode;
        this.priority(dto.Priority);
        this.configuration(this.parseConfiguration(dto.Configuration));

        this.additionalSources(_.map(dto.AdditionalSources, (code, name) => additionalSource.create(name, code)));

        this.hasDuplicateFieldsNames = ko.pureComputed(() => {
            return _.uniqBy(this.fields(), field => field.name()).length !== this.fields().length;
        });
        
        if (!this.isAutoIndex()) {
            this.initValidation();
        }
    } 
    
    private initValidation() {        
        
        const checkIndexName = (val: string,
                                params: any,
                                callback: (currentValue: string, errorMessageOrValidationResult: string | boolean) => void) => {
                                    new validateNameCommand('Index', val)
                                        .execute()
                                        .done((result) => {
                                            if (result.IsValid) {
                                                callback(this.name(), true);
                                            } else {
                                                callback(this.name(), result.ErrorMessage);
                                            }
                                        })
                               };
        
        this.name.extend({
            required: true,
            validation: [
                {
                    async: true,
                    validator: generalUtils.debounceAndFunnel(checkIndexName)
                }]
        });
        
        this.fields.extend({
           validation: [
               {
                   validator: () => !this.hasDuplicateFieldsNames()
               }
           ] 
        });

        this.reduce.extend({
            required: {
                onlyIf: () => this.hasReduce() && !this.reduce()
            },
            validation: [
                {
                    validator: (reduceContent: string) =>  (this.hasReduce() && reduceContent && reduceContent.trim()) ||
                                                           !this.hasReduce(),
                    message: `Reduce function is empty`
                }
            ]
        });

        this.reduceOutputCollectionName.extend({
            required: {
                onlyIf: () => this.hasReduce() && this.outputReduceToCollection()
            }
        });

        this.patternForReferencesToReduceOutputCollection.extend({
            required: {
                onlyIf: () => this.hasReduce() && this.createReferencesToResultsCollection()
            }
        });
        
        this.collectionNameForReferenceDocuments.extend({
            validation: [
                {
                    validator: (value: string) => !value || value !== this.reduceOutputCollectionName(),
                    message: 'Name for Referencing Collection cannot be the same as the Reduce Output Collection.'
                }
            ]
        });

        this.validationGroup = ko.validatedObservable({
            name: this.name,
            reduce: this.reduce,
            reduceOutputCollectionName: this.reduceOutputCollectionName,
            patternForReferencesToReduceOutputCollection: this.patternForReferencesToReduceOutputCollection,
            collectionNameForReferenceDocuments: this.collectionNameForReferenceDocuments,
            fields: this.fields
        });
    }

    private parseConfiguration(config: Raven.Client.Documents.Indexes.IndexConfiguration): Array<configurationItem> {
        const configurations = [] as configurationItem[];

        if (config) {
            _.forIn(config, (value, key) => {
                configurations.push(new configurationItem(key, value));
            });
        }

        return configurations;
    }

    private detectIndexType(): Raven.Client.Documents.Indexes.IndexType {
        return this.reduce() ? "MapReduce" : "Map";
    }

    private fieldToDto(): dictionary<Raven.Client.Documents.Indexes.IndexFieldOptions> {
        const fields = {} as dictionary<Raven.Client.Documents.Indexes.IndexFieldOptions>;

        this.fields().forEach((indexField: indexFieldOptions) => {
            fields[indexField.name()] = indexField.toDto();
        });

        if (this.defaultFieldOptions()) {
            fields[indexFieldOptions.DefaultFieldOptions] = this.defaultFieldOptions().toDto();
        }

        return fields;
    }

    private configurationToDto(): Raven.Client.Documents.Indexes.IndexConfiguration {
        const result = {} as Raven.Client.Documents.Indexes.IndexConfiguration;

        this.configuration().forEach((configItem: configurationItem) => {
            result[configItem.key()] = configItem.value();
        });

        return result;
    }
    
    private additionalSourceToDto(): dictionary<string> {
        if (!this.additionalSources().length) {
            return null;  
        }
        const result = {} as dictionary<string>;
        
        this.additionalSources().forEach(source => {
            result[source.name()] = source.code();
        });
        
        return result;
    }

    toDto(): Raven.Client.Documents.Indexes.IndexDefinition {
        return {
            Name: this.name(),
            Maps: this.maps().map(m => m.map()),
            Reduce: this.reduce(),
            Type: this.detectIndexType(),
            SourceType: "None",
            LockMode: this.lockMode,
            Priority: this.priority(),
            Configuration: this.configurationToDto(),
            Fields: this.fieldToDto(),
            OutputReduceToCollection: this.hasReduce() &&
                                      this.reduce()    &&
                                      this.outputReduceToCollection() ? 
                                           this.reduceOutputCollectionName() : null,
            PatternForOutputReduceToCollectionReferences: this.hasReduce() &&
                                                          this.reduce()    &&
                                                          this.outputReduceToCollection() &&
                                                          this.createReferencesToResultsCollection() && 
                                                          this.patternForReferencesToReduceOutputCollection() ? 
                                                               this.patternForReferencesToReduceOutputCollection() : null,
            PatternReferencesCollectionName: this.hasReduce() &&
                                             this.reduce()    &&
                                             this.outputReduceToCollection() &&
                                             this.createReferencesToResultsCollection() &&
                                             this.collectionNameForReferenceDocuments() ?
                                                this.collectionNameForReferenceDocuments() : null,
            AdditionalSources: this.additionalSourceToDto()
        }
    }

    addMap() {
        const map = new mapItem("");
        this.maps.push(map);
    }

    addField() {
        const field = indexFieldOptions.empty();
        if (this.defaultFieldOptions()) {
            field.parent(this.defaultFieldOptions());
        }
        this.fields.push(field);
    }

    addDefaultField() {
        const fieldOptions = indexFieldOptions.defaultFieldOptions();
        this.defaultFieldOptions(fieldOptions);

        this.fields().forEach(field => {
            field.parent(fieldOptions);
        });
    }

    addConfigurationOption() {
        this.configuration.push(configurationItem.empty());
    }

    removeConfigurationOption(item: configurationItem) {
        this.configuration.remove(item);
    }

    removeDefaultFieldOptions() {
        this.defaultFieldOptions(null);

        this.fields().forEach(field => {
            field.parent(indexFieldOptions.defaultFieldOptions());
        });
    }

    static empty(): indexDefinition {
        return new indexDefinition({
            Fields: {},
            Maps: [""],
            Name: "",
            LockMode: "Unlock",
            Reduce: undefined,
            Priority: "Normal",
            Configuration: null,
            Type: "Map",
            SourceType: "None",
            OutputReduceToCollection: null,
            AdditionalSources: null,
            PatternForOutputReduceToCollectionReferences: null,
            PatternReferencesCollectionName: null
        });
    }
}

export = indexDefinition;
